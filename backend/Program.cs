
using Backend.Features.Health;
using Backend.Features.Lookup;
using Backend.Features.Search;
using Backend.Features.Users;
using Backend.Features.Recommendations;
using Backend.Features.Recommendations.Endpoints;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Backend.Providers.GoogleBooks;
using Backend.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL database.
builder.Services.AddDbContext<PaletteDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// Configure OpenAPI.
builder.Services.AddOpenApi();

// Register error handling.
builder.Services.AddApiErrorHandling();

// Register providers and application services.
builder.Services.AddMediaProviders();
builder.Services.AddTmdbProvider(builder.Configuration);
builder.Services.AddGoogleBooksProvider(builder.Configuration);

builder.Services.AddSearchFeature();
builder.Services.AddLookupFeature();
builder.Services.AddLoggingFeature();
builder.Services.AddUserService();

// Register the original recommendation engine integration.
builder.Services.AddRecommendationFeature();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:8081")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();

// Configure Clerk JWT authentication.
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority =
            builder.Configuration["Clerk:FrontendAPIURL"]
            ?? throw new InvalidOperationException(
                "Clerk:FrontendAPIURL is not configured.");

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateIssuer = true
            };
    });

// Require authentication by default.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

// Configure error handling.
app.UseExceptionHandler();

app.UseCors("FrontendDev");

// Development configuration.
if (app.Environment.IsDevelopment())
{
    // Apply pending database migrations.
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PaletteDbContext>();

        dbContext.Database.Migrate();
    }

    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<EnsureUserExistsMiddleware>();

app.MapGet("/api/test", () =>
    Results.Ok(new
    {
        message = "Connected to backend!"
    })
).AllowAnonymous();

// Existing endpoints.
app.MapLibrary();
app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapLookupEndpoints();
app.MapSearchInteractionEndpoints();

// Personalized movie recommendations.
app.MapRecommendationEndpoints();

app.Run();

public partial class Program { }