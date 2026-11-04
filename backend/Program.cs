using Microsoft.AspNetCore.Authentication.JwtBearer;
using Backend.Features.Health;
using Backend.Features.Lookup;
using Backend.Features.Search;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Backend.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Palette.Api.Data;
using Palette.Api.Logging;
using Palette.Api.Endpoints;
using Palette.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var clerkIssuer = builder.Configuration["AUTH_CLERK_URL"]
    ?? builder.Configuration["Authentication:Clerk:Issuer"];
if (string.IsNullOrWhiteSpace(clerkIssuer))
{
    throw new InvalidOperationException(
        "Configure AUTH_CLERK_URL with your Clerk issuer URL.");
}

var database = Environment.GetEnvironmentVariable("POSTGRES_DB");
var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
var hasDatabaseEnvironment = new[] { database, username, password }
    .Any(value => !string.IsNullOrWhiteSpace(value));

string connectionString;
if (hasDatabaseEnvironment)
{
    if (string.IsNullOrWhiteSpace(database)
        || string.IsNullOrWhiteSpace(username)
        || string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "POSTGRES_DB, POSTGRES_USER, and POSTGRES_PASSWORD must all be configured.");
    }

    var portValue = Environment.GetEnvironmentVariable("POSTGRES_PORT");
    var port = string.IsNullOrWhiteSpace(portValue)
        ? 5433
        : int.TryParse(portValue, out var parsedPort)
            ? parsedPort
            : throw new InvalidOperationException("POSTGRES_PORT must be a valid port number.");

    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = "localhost",
        Port = port,
        Database = database,
        Username = username,
        Password = password,
    }.ConnectionString;
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Set POSTGRES_DB, POSTGRES_USER, and POSTGRES_PASSWORD, or configure the DefaultConnection connection string.");
}

builder.Services.AddDbContext<PaletteDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure OpenAPI
builder.Services.AddOpenApi();
// Logging uses the shared Clerk authentication configured below, not demo authentication.
builder.Services.AddScoped<ShowLogService>();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = clerkIssuer;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization();

// Register ProblemDetails and ApiExceptionHandler (required by app.UseExceptionHandler)
builder.Services.AddApiErrorHandling();

// Register media providers and the search feature
builder.Services.AddMediaProviders();
builder.Services.AddTmdbProvider(builder.Configuration);
builder.Services.AddSearchFeature();
builder.Services.AddLookupFeature();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:8081")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

//adding the api exception handler.
app.UseExceptionHandler();

app.UseCors("FrontendDev");
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

// Development configuration
if (app.Environment.IsDevelopment())
{
    // Automatically apply pending database migrations
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PaletteDbContext>();

        dbContext.Database.Migrate();
    }

    app.MapOpenApi();
}

app.MapShowLogs();

app.MapGet("/api/test", () =>
    Results.Ok(new { message = "Connected to backend!" })
)
    .WithName("TestConnection")
    .WithSummary("Check API connectivity");

app.MapCurrentUserEndpoints();

app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapLookupEndpoints();

app.Run();

public partial class Program { }
