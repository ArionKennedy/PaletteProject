using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Palette.Tests.Unit.Errors;

public class ApiExceptionHandlerTests
{
    [Theory]
    [InlineData("invalid-request", 400, LogLevel.Information)]
    [InlineData("not-found", 404, LogLevel.Information)]
    [InlineData("unknown-media-type", 400, LogLevel.Information)]
    [InlineData("provider-unavailable", 502, LogLevel.Error)]
    public async Task KnownApiExceptionWritesProblemDetailsAndLogsAtExpectedLevel(
        string exceptionType,
        int expectedStatus,
        LogLevel expectedLogLevel)
    {
        using var services = CreateServices();
        using var responseBody = new MemoryStream();

        var context = CreateContext(services, responseBody);
        context.TraceIdentifier = "test-trace-123";

        var logger = new RecordingLogger();
        var handler = new ApiExceptionHandler(logger);
        var exception = CreateApiException(exceptionType);

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);

        // WriteAsJsonAsync may append a charset to the content type.
        var contentType = context.Response.ContentType;

        Assert.NotNull(contentType);
        Assert.Equal(
            "application/problem+json",
            contentType.Split(';')[0].Trim());

        var body = await ReadBodyAsync(responseBody);

        using var document = JsonDocument.Parse(body);
        var problem = document.RootElement;

        Assert.Equal(
            expectedStatus,
            problem.GetProperty("status").GetInt32());
        Assert.Equal(
            exception.Title,
            problem.GetProperty("title").GetString());
        Assert.Equal(
            exception.Detail,
            problem.GetProperty("detail").GetString());
        Assert.Equal(
            "test-trace-123",
            problem.GetProperty("traceId").GetString());

        var log = Assert.Single(logger.Entries);

        Assert.Equal(expectedLogLevel, log.Level);
        Assert.False(string.IsNullOrWhiteSpace(log.Message));

        if (expectedLogLevel == LogLevel.Error)
        {
            // Server-side logs retain the exception for diagnosis.
            Assert.Same(exception, log.Exception);
        }
        else
        {
            Assert.Null(log.Exception);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnexpectedExceptionIsNotHandledOrWrittenToResponse(
        bool useHttpRequestException)
    {
        using var services = CreateServices();
        using var responseBody = new MemoryStream();

        var context = CreateContext(services, responseBody);
        var logger = new RecordingLogger();
        var handler = new ApiExceptionHandler(logger);

        Exception exception = useHttpRequestException
            ? new HttpRequestException("Unexpected HTTP failure.")
            : new InvalidOperationException("Unexpected programming error.");

        var originalStatus = context.Response.StatusCode;
        var originalContentType = context.Response.ContentType;

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(originalStatus, context.Response.StatusCode);
        Assert.Equal(
            originalContentType,
            context.Response.ContentType);
        Assert.Equal(0L, responseBody.Length);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task UnexpectedExceptionPreservesExistingResponseState()
    {
        using var services = CreateServices();
        using var responseBody = new MemoryStream();

        var existingBody = Encoding.UTF8.GetBytes("Existing response");

        await responseBody.WriteAsync(existingBody);

        var context = CreateContext(services, responseBody);
        context.Response.StatusCode = StatusCodes.Status202Accepted;
        context.Response.ContentType = "text/plain";

        var logger = new RecordingLogger();
        var handler = new ApiExceptionHandler(logger);

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Unexpected error."),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(
            StatusCodes.Status202Accepted,
            context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Equal(
            "Existing response",
            await ReadBodyAsync(responseBody));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ProviderFailureDoesNotLeakInternalDetailsToClient()
    {
        using var services = CreateServices();
        using var responseBody = new MemoryStream();

        var context = CreateContext(services, responseBody);
        context.TraceIdentifier = "provider-failure-trace";

        var original = new HttpRequestException(
            "sensitive-upstream-marker: internal connection details");

        var exception = new ProviderUnavailableException(
            "internal-provider-name",
            original);

        var logger = new RecordingLogger();
        var handler = new ApiExceptionHandler(logger);

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status502BadGateway,
            context.Response.StatusCode);

        var body = await ReadBodyAsync(responseBody);

        Assert.DoesNotContain("sensitive-upstream-marker", body);
        Assert.DoesNotContain("internal connection details", body);
        Assert.DoesNotContain("internal-provider-name", body);

        using var document = JsonDocument.Parse(body);
        var problem = document.RootElement;

        Assert.Equal(
            "Provider unavailable",
            problem.GetProperty("title").GetString());
        Assert.Equal(
            "The media provider is currently unavailable. Please try again later.",
            problem.GetProperty("detail").GetString());
        Assert.Equal(
            "provider-failure-trace",
            problem.GetProperty("traceId").GetString());

        // Internal details remain available to server-side logging.
        var log = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Same(exception, log.Exception);
        Assert.Same(original, log.Exception!.InnerException);
    }

    private static ApiException CreateApiException(string exceptionType)
    {
        return exceptionType switch
        {
            "invalid-request" => new InvalidRequestException(
                "Query parameter 'q' must not be empty."),

            "not-found" => new MediaNotFoundException(
                "movie",
                "42"),

            "unknown-media-type" => new UnknownMediaTypeException(
                "unregistered",
                new[] { "movie", "tv" }),

            "provider-unavailable" => new ProviderUnavailableException(
                "test-provider",
                new HttpRequestException("Upstream failure.")),

            _ => throw new ArgumentOutOfRangeException(
                nameof(exceptionType),
                exceptionType,
                "Unknown test exception type."),
        };
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        // Supply the JSON options used by HttpResponse.WriteAsJsonAsync.
        services.Configure<JsonOptions>(_ => { });

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(
        IServiceProvider services,
        Stream responseBody)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };

        context.Response.Body = responseBody;

        return context;
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        body.Position = 0;

        using var reader = new StreamReader(
            body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        return await reader.ReadToEndAsync();
    }

    private sealed record LogEntry(
        LogLevel Level,
        Exception? Exception,
        string Message);

    private sealed class RecordingLogger : ILogger<ApiExceptionHandler>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(
                logLevel,
                exception,
                formatter(state, exception)));
        }
    }
}