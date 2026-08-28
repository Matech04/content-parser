using System.Threading.RateLimiting;

namespace ContentParser.Api.Endpoints;

public static class RateLimiting
{
    public const string PolicyName = "api";

    /// <summary>
    /// Okno partycjonowane po adresie klienta. Wspolny kubelek dla wszystkich
    /// pozwolilby jednemu klientowi zaglodzic pozostalych.
    /// </summary>
    public static IServiceCollection AddApiRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            // Domyslnie limiter odpowiada 503; poprawnym statusem wg RFC 6585 jest 429.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PolicyName, static context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),

                        // Bez kolejki: nadmiarowe zadanie ma dostac 429 od razu,
                        // zamiast trzymac polaczenie otwarte przez cale okno.
                        QueueLimit = 0,
                    }));
        });
}
