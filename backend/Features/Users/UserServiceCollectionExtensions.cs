namespace Backend.Features.Users;

public static class UserServiceCollectionExtensions
{
    public static IServiceCollection AddUserService(this IServiceCollection services)
    {
        services.AddScoped<UserService>();

        return services;
    }
}
