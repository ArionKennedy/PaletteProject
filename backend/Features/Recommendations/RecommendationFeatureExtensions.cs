using Backend.Features.Recommendations.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Features.Recommendations;

public static class RecommendationFeatureExtensions
{
    // Registers the services required by the recommendation feature.
    public static IServiceCollection AddRecommendationFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ReviewStatisticsService>();

        return services;
    }
}