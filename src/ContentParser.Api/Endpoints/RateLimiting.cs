using System.Threading.RateLimiting;

namespace ContentParser.Api.Endpoints;

public static class RateLimiting
{
    public const string PolicyName = "api";

    public static IServiceCollection AddApiRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PolicyName, static context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),

                        QueueLimit = 0,
                    }));
        });
}
