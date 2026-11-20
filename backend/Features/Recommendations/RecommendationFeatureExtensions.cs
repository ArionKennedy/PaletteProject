
using Backend.Features.Recommendations.Data;
using Backend.Features.Recommendations.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Features.Recommendations;

public static class RecommendationFeatureExtensions
{
    public static IServiceCollection AddRecommendationFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ReviewStatisticsService>();
        services.AddScoped<MediaMetadataService>();
        services.AddScoped<UserInteractionService>();
        services.AddScoped<SearchInteractionService>();

        services.AddScoped<MovieCandidateDiscoveryService>();
        services.AddScoped<MovieCandidateFilterService>();

        services.AddScoped<RecommendationOrchestrator>();

        return services;
    }
}
