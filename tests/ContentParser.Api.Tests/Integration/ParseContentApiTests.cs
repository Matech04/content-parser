using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ContentParser.Api.Tests.Integration;

/// <summary>
/// Testy przez pelny potok HTTP: bindowanie, negocjacja Content-Type, obsluga wyjatkow
/// i serializacja. Sprawdzaja kontrakt, ktorego testy jednostkowe endpointu nie dotykaja.
/// </summary>
public class ParseContentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Url = "/api/v1/parse-content";

    private readonly WebApplicationFactory<Program> _factory;

    public ParseContentApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static string ToBase64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Csv_RoundTripsThroughTheWholePipeline()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync(Url, new { type = "CSV", content = ToBase64("id,name\n1,Anna\n2,Piotr") });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Success", payload.GetProperty("status").GetString());
        Assert.Equal(2, payload.GetProperty("processedCount").GetInt32());
    }

    [Fact]
    public async Task InternalJson_RoundTripsThroughTheWholePipeline()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync(Url, new { type = "INTERNAL_JSON", content = ToBase64("""[{"id":"1"}]""") });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingTypeField_Returns400_NotServerError()
    {
        var response = await _factory.CreateClient().PostAsync(Url, Json("""{"content":"W10="}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyJsonBody_Returns400_NotServerError()
    {
        var response = await _factory.CreateClient().PostAsync(Url, Json("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public async Task WrongContentType_Returns415(string contentType)
    {
        var body = new StringContent("""{"type":"CSV","content":"aWQKMQ=="}""", Encoding.UTF8, contentType);

        var response = await _factory.CreateClient().PostAsync(Url, body);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task ErrorResponses_UseProblemDetails()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync(Url, new { type = "XML", content = "W10=" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith("unsupported-parser", problem.GetProperty("type").GetString());
        Assert.Single(problem.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task GetOnPostOnlyRoute_Returns405()
    {
        var response = await _factory.CreateClient().GetAsync(Url);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
