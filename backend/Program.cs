using Backend.Features.Health;
using Backend.Features.Lookup;
using Backend.Features.Search;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Backend.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL database
builder.Services.AddDbContext<PaletteDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddLoggingFeature(builder.Configuration, builder.Environment);

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapShowLogs();

app.MapGet("/api/test", () =>
    Results.Ok(new { message = "Connected to backend!" })
);

app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapLookupEndpoints();

app.Run();

public partial class Program { }
