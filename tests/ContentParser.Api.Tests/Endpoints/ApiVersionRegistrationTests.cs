using ContentParser.Api.Endpoints;

using ContentParser.Core.Parsers;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Parsers.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ContentParser.Api.Tests.Endpoints;

public class ApiVersionRegistrationTests
{
    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.Services.AddLogging();
        builder.Services.AddOptions<ParsingOptions>();
        builder.Services.AddSingleton<IContentParser, InternalJsonParser>();
        builder.Services.AddSingleton<IContentParser, CsvParser>();
        builder.Services.AddSingleton<Base64Decoder>();
        builder.Services.AddSingleton<ContentParsingService>();

        return builder.Build();
    }

    private static IReadOnlyList<RouteEndpoint> MapAndCollect()
    {
        var app = BuildApp();
        app.MapApiEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static RouteEndpoint ParseContentEndpoint() =>
        Assert.Single(MapAndCollect(), e => e.RoutePattern.RawText == "/api/v1/parse-content");

    [Fact]
    public void MapApiEndpoints_RegistersParseContentUnderV1()
    {
        Assert.Contains(MapAndCollect(), e => e.RoutePattern.RawText == "/api/v1/parse-content");
    }

    [Fact]
    public void ParseContent_IsExposedAsPost()
    {
        var methods = ParseContentEndpoint().Metadata.GetMetadata<HttpMethodMetadata>();

        Assert.NotNull(methods);
        Assert.Equal(["POST"], methods.HttpMethods);
    }

    [Fact]
    public void ParseContent_IsRateLimitedByApiPolicy()
    {
        var rateLimiting = ParseContentEndpoint().Metadata.GetMetadata<EnableRateLimitingAttribute>();

        Assert.NotNull(rateLimiting);
        Assert.Equal(RateLimiting.PolicyName, rateLimiting.PolicyName);
    }

    [Fact]
    public void ParseContent_IsTaggedAsV1()
    {
        var tags = ParseContentEndpoint().Metadata.GetMetadata<ITagsMetadata>();

        Assert.NotNull(tags);
        Assert.Contains("v1", tags.Tags);
    }

    [Fact]
    public void ParseContent_AcceptsOnlyApplicationJson()
    {
        var accepts = ParseContentEndpoint().Metadata.GetMetadata<IAcceptsMetadata>();

        Assert.NotNull(accepts);
        Assert.Equal(["application/json"], accepts.ContentTypes);
    }

    [Fact]
    public void MapApiEndpoints_ReturnsSameApplication_ForChaining()
    {
        var app = BuildApp();

        Assert.Same(app, app.MapApiEndpoints());
    }
}
