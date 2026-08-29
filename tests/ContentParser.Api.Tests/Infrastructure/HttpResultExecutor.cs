using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ContentParser.Api.Tests.Infrastructure;

internal static class HttpResultExecutor
{
    private static readonly IServiceProvider Services = BuildServices();

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddProblemDetails();
        return services.BuildServiceProvider();
    }

    public static async Task<ExecutedResult> ExecuteAsync(IResult result)
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext
        {
            RequestServices = Services,
        };
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        body.Position = 0;
        var text = await new StreamReader(body).ReadToEndAsync();

        return new ExecutedResult(context.Response.StatusCode, context.Response.ContentType, text);
    }

    internal sealed record ExecutedResult(int StatusCode, string? ContentType, string Body)
    {
        public JsonElement Json => JsonDocument.Parse(Body).RootElement;

        public string? GetString(string property) =>
            Json.TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
