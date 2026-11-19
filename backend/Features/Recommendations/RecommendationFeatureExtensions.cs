
using Backend.Features.Recommendations.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Features.Recommendations;

public static class RecommendationFeatureExtensions
{
    // Registers the recommendation feature services.
    public static IServiceCollection AddRecommendationFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ReviewStatisticsService>();
        services.AddScoped<MediaMetadataService>();
        services.AddScoped<UserInteractionService>();

        return services;
    }
}