namespace Backend.Features.Users;

public sealed class EnsureUserExistsMiddleware
{
    private readonly RequestDelegate _next;

    public EnsureUserExistsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserService userService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var clerkId = userService.GetCurrentClerkId();

            if (!string.IsNullOrWhiteSpace(clerkId) && !await userService.UserExistsAsync(clerkId, context.RequestAborted))
            {
                await userService.SaveCurrentUserAsync(context.RequestAborted);
            }
        }

        await _next(context);
    }
}
