using System.Text;

using ContentParser.Api.Contracts.V1;
using ContentParser.Api.Endpoints.V1;
using ContentParser.Api.Tests.Infrastructure;

using ContentParser.Core.Parsers;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Parsers.Services;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContentParser.Api.Tests.Endpoints.V1;

public class ParseContentEndpointTests
{
    private static ContentParsingService CreateService(int maxDecodedContentBytes = 5 * 1024 * 1024)
    {
        var options = Options.Create(new ParsingOptions { MaxDecodedContentBytes = maxDecodedContentBytes });

        return new ContentParsingService(
            [new InternalJsonParser(options), new CsvParser(options)],
            new Base64Decoder(options),
            NullLogger<ContentParsingService>.Instance);
    }

    private static string ToBase64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static Task<HttpResultExecutor.ExecutedResult> Post(string? type, string? content, int maxBytes = 5 * 1024 * 1024) =>
        HttpResultExecutor.ExecuteAsync(
            ParseContentEndpoint.Handle(new ParseContentRequestDto(type, content), CreateService(maxBytes)));

    [Fact]
    public async Task Json_ValidRequest_Returns200WithUnifiedPayload()
    {
        var response = await Post("INTERNAL_JSON", ToBase64("""[{"id":"1","name":"Anna"}]"""));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("Success", response.GetString("status"));
        Assert.Equal(1, response.Json.GetProperty("processedCount").GetInt32());
        Assert.Equal("Anna", response.Json.GetProperty("data")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Csv_ValidRequest_Returns200WithUnifiedPayload()
    {
        var response = await Post("CSV", ToBase64("id,name\n1,Anna\n2,Piotr"));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("Success", response.GetString("status"));
        Assert.Equal(2, response.Json.GetProperty("processedCount").GetInt32());
        Assert.Equal("Piotr", response.Json.GetProperty("data")[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task BothTypes_ProduceIdenticalResponseBody()
    {
        var fromCsv = await Post("CSV", ToBase64("id,name\n1,Anna"));
        var fromJson = await Post("INTERNAL_JSON", ToBase64("""[{"id":"1","name":"Anna"}]"""));

        Assert.Equal(fromCsv.Body, fromJson.Body);
    }

    [Theory]
    [InlineData("XML")]
    [InlineData("yaml")]
    public async Task UnknownType_Returns400(string type)
    {
        var response = await Post(type, ToBase64("[]"));

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.EndsWith("unsupported-parser", response.GetString("type"));
    }

    [Fact]
    public async Task MissingType_Returns400_InsteadOfCrashing()
    {
        var response = await Post(type: null, content: ToBase64("[]"));

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.EndsWith("type-missing", response.GetString("type"));
    }

    [Fact]
    public async Task InvalidBase64_Returns422()
    {
        var response = await Post("CSV", "!!!nie-base64!!!");

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.EndsWith("incorrect-base64", response.GetString("type"));
    }

    [Fact]
    public async Task JsonThatIsNotAnArray_Returns422()
    {
        var response = await Post("INTERNAL_JSON", ToBase64("""{"id":1}"""));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.EndsWith("json-not-array", response.GetString("type"));
    }

    [Fact]
    public async Task MalformedCsv_Returns422_WithEveryOffendingRow()
    {
        var response = await Post("CSV", ToBase64("id,name\n1\n2"));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal(2, response.Json.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task ContentOverLimit_Returns413()
    {
        var response = await Post("INTERNAL_JSON", ToBase64($"[{new string('a', 4096)}]"), maxBytes: 64);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task EmptyContent_Returns422(string? content)
    {
        var response = await Post("CSV", content);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.EndsWith("content-empty", response.GetString("type"));
    }
}
