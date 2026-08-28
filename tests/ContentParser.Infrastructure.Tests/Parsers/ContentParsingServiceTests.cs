using System.Text;

using ContentParser.Infrastructure.Tests.TestDoubles;

using ContentParser.Parser.Parsers;
using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Parsers.Services;
using ContentParser.Parser.Results.Errors;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContentParser.Infrastructure.Tests.Parsers;

public class ContentParsingServiceTests
{
    private const string InternalJson = "INTERNAL_JSON";

    private static string ToBase64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static IOptions<ParsingOptions> Opts(int maxDecodedContentBytes = 5 * 1024 * 1024) =>
        Options.Create(new ParsingOptions { MaxDecodedContentBytes = maxDecodedContentBytes });

    private static ContentParsingService CreateSut(params IContentParser[] parsers) =>
        new(
            parsers.Length == 0 ? [new InternalJsonParser(Opts()), new CsvParser(Opts())] : parsers,
            new Base64Decoder(Opts()),
            NullLogger<ContentParsingService>.Instance);

    [Fact]
    public void ParseContent_InternalJson_ReturnsParsedRecords()
    {
        var result = CreateSut().ParseContent(InternalJson, ToBase64("""[{"id":1},{"id":2},{"id":3}]"""));

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(3, parsed.ProcessedCount);
    }

    [Fact]
    public void ParseContent_Csv_ReturnsParsedRecords()
    {
        var result = CreateSut().ParseContent("CSV", ToBase64("id,name\n1,Anna\n2,Piotr"));

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(2, parsed.ProcessedCount);
    }

    [Fact]
    public void ParseContent_BothTypes_ProduceTheSameStructure()
    {
        var sut = CreateSut();

        sut.ParseContent("CSV", ToBase64("id,name\n1,Anna")).TryGetValue(out var fromCsv);
        sut.ParseContent(InternalJson, ToBase64("""[{"id":"1","name":"Anna"}]""")).TryGetValue(out var fromJson);

        Assert.Equal(fromCsv!.ProcessedCount, fromJson!.ProcessedCount);
        Assert.Equal(fromCsv.Records[0].Fields, fromJson.Records[0].Fields);
    }

    [Theory]
    [InlineData("internal_json")]
    [InlineData("Internal_Json")]
    [InlineData("csv")]
    public void ParseContent_TypeMatchingIsCaseInsensitive(string type)
    {
        var content = type.StartsWith("csv", StringComparison.OrdinalIgnoreCase) ? "id\n1" : "[]";

        Assert.True(CreateSut().ParseContent(type, ToBase64(content)).IsSuccess);
    }

    [Theory]
    [InlineData("XML")]
    [InlineData("INTERNAL_JSON ")]
    [InlineData("json")]
    public void ParseContent_UnknownType_FailsWithUnsupportedParser(string type)
    {
        var result = CreateSut().ParseContent(type, ToBase64("[]"));

        var error = Assert.IsType<RequestError.UnsupportedParser>(Assert.Single(result.Errors));
        Assert.Contains("CSV", error.Message);
        Assert.Contains(InternalJson, error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseContent_MissingType_FailsWithTypeIsMissing(string? type)
    {
        var result = CreateSut().ParseContent(type, ToBase64("[]"));

        Assert.IsType<RequestError.TypeIsMissing>(Assert.Single(result.Errors));
    }

    [Fact]
    public void ParseContent_UnknownType_DoesNotTouchTheContent()
    {
        var stub = new StubContentParser(InternalJson);

        var result = CreateSut(stub).ParseContent("XML", "!!! nie-base64 !!!");

        Assert.False(result.IsSuccess);
        Assert.Equal(0, stub.CallCount);
    }

    [Fact]
    public void ParseContent_InvalidBase64_PropagatesDecodingError_WithoutCallingParser()
    {
        var stub = new StubContentParser(InternalJson);

        var result = CreateSut(stub).ParseContent(InternalJson, "!!!nie-base64!!!");

        Assert.IsType<ValidationError.IncorrectContentBase64Encoding>(Assert.Single(result.Errors));
        Assert.Equal(0, stub.CallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseContent_EmptyContent_FailsWithContentIsEmpty(string? content)
    {
        var result = CreateSut().ParseContent(InternalJson, content);

        Assert.IsType<ValidationError.ContentIsEmpty>(Assert.Single(result.Errors));
    }

    [Fact]
    public void ParseContent_PassesDecodedContentToParser()
    {
        var stub = new StubContentParser(InternalJson);

        CreateSut(stub).ParseContent(InternalJson, ToBase64("""[{"id":1}]"""));

        Assert.Equal("""[{"id":1}]""", stub.LastContent);
        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public void ParseContent_PropagatesParserError()
    {
        var error = new ValidationError.JsonIsNotAnArray();
        var stub = StubContentParser.Failing(InternalJson, error);

        var result = CreateSut(stub).ParseContent(InternalJson, ToBase64("{}"));

        Assert.Equal(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void ParseContent_ContentOverLimit_FailsWithTooLarge()
    {
        var sut = new ContentParsingService(
            [new InternalJsonParser(Opts())],
            new Base64Decoder(Opts(maxDecodedContentBytes: 64)),
            NullLogger<ContentParsingService>.Instance);

        var result = sut.ParseContent(InternalJson, ToBase64($"[{new string('a', 4096)}]"));

        Assert.IsType<TooLargeError.ContentIsTooLarge>(Assert.Single(result.Errors));
    }

    [Fact]
    public void ParseContent_RoutesToTheParserMatchingTheType()
    {
        var json = new StubContentParser(InternalJson);
        var csv = new StubContentParser("CSV");

        CreateSut(json, csv).ParseContent("CSV", ToBase64("a,b"));

        Assert.Equal(0, json.CallCount);
        Assert.Equal(1, csv.CallCount);
    }

    [Fact]
    public void SupportedTypes_ListsEveryRegisteredParser()
    {
        Assert.Equal(["CSV", InternalJson], CreateSut().SupportedTypes);
    }

    [Fact]
    public void Constructor_WithNoParsers_DoesNotThrow()
    {
        var sut = new ContentParsingService([], new Base64Decoder(Opts()), NullLogger<ContentParsingService>.Instance);

        Assert.IsType<RequestError.UnsupportedParser>(Assert.Single(sut.ParseContent(InternalJson, ToBase64("[]")).Errors));
    }
}
