using Backend.Features.Health;
using Backend.Features.Lookup;
using Backend.Features.Search;
using Backend.Features.Users;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Backend.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure PostgreSQL database
builder.Services.AddDbContext<PaletteDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure OpenAPI
builder.Services.AddOpenApi();

// Register ProblemDetails and ApiExceptionHandler (required by app.UseExceptionHandler)
builder.Services.AddApiErrorHandling();

// Register media providers and the search feature
builder.Services.AddMediaProviders();
builder.Services.AddTmdbProvider(builder.Configuration);
builder.Services.AddSearchFeature();
builder.Services.AddLookupFeature();
builder.Services.AddUserService();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Clerk:FrontendAPIURL"] ?? throw new InvalidOperationException("Clerk:FrontendAPIURL is not configured.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidateIssuer = true
        };
    });


// set all endpoints to require authorization unless marked with [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<EnsureUserExistsMiddleware>();

app.MapGet("/api/test", () =>
    Results.Ok(new { message = "Connected to backend!" })
).AllowAnonymous();

app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapLookupEndpoints();

app.Run();
