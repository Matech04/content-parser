using ContentParser.Api.Endpoints.V1;

namespace ContentParser.Api.Endpoints;

public static class ApiVersionRegistration
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1")
                    .WithTags("v1")
                    .WithGroupName("v1")
                    .RequireRateLimiting(RateLimiting.PolicyName);

        ParseContentEndpoint.Map(v1);

        return app;
    }
}
