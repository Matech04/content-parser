using ContentParser.Core.Parsers;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Core.Tests.Parsers;

public class InternalJsonParserTests
{
    private static InternalJsonParser CreateSut(int maxRecords = 100_000) =>
        new(Options.Create(new ParsingOptions { MaxRecords = maxRecords }));

    private readonly InternalJsonParser _sut = CreateSut();

    [Fact]
    public void Type_IsInternalJson()
    {
        Assert.Equal("INTERNAL_JSON", _sut.Type);
    }

    [Fact]
    public void TryParse_EmptyArray_ReturnsZeroRecords()
    {
        var result = _sut.TryParse("[]");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(0, parsed.ProcessedCount);
        Assert.Empty(parsed.Records);
    }

    [Fact]
    public void TryParse_ArrayOfObjects_CountsAndFlattensRecords()
    {
        var result = _sut.TryParse("""[{"id":1,"name":"Anna"},{"id":2,"name":"Piotr"}]""");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(2, parsed.ProcessedCount);
        Assert.Equal("1", parsed.Records[0].Fields["id"]);
        Assert.Equal("Anna", parsed.Records[0].Fields["name"]);
        Assert.Equal("Piotr", parsed.Records[1].Fields["name"]);
    }

    [Fact]
    public void TryParse_StringsAreNotQuotedTwice()
    {
        var result = _sut.TryParse("""[{"name":"Anna"}]""");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal("Anna", parsed.Records[0].Fields["name"]);
    }

    [Fact]
    public void TryParse_JsonNullValue_BecomesNullField()
    {
        var result = _sut.TryParse("""[{"name":null}]""");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Null(parsed.Records[0].Fields["name"]);
    }

    [Theory]
    [InlineData("""[{"flag":true}]""", "true")]
    [InlineData("""[{"amount":12.5}]""", "12.5")]
    public void TryParse_ScalarsAreRenderedAsText(string json, string expected)
    {
        var result = _sut.TryParse(json);

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(expected, parsed.Records[0].Fields.Values.Single());
    }

    [Fact]
    public void TryParse_IgnoresSurroundingWhitespace()
    {
        var result = _sut.TryParse("""   [{"id":1}]   """);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("""{"id":1}""")]
    [InlineData("\"just a string\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void TryParse_NonArrayJson_FailsWithJsonIsNotAnArray(string json)
    {
        var result = _sut.TryParse(json);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e is ValidationError.JsonIsNotAnArray);
    }

    [Theory]
    [InlineData("to nie jest json")]
    [InlineData("[1, 2,]")]
    [InlineData("[1, 2")]
    [InlineData("[] []")]
    [InlineData("{")]
    public void TryParse_MalformedJson_FailsWithIncorrectJson(string json)
    {
        var result = _sut.TryParse(json);

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.IncorrectJson>(Assert.Single(result.Errors));
    }

    [Fact]
    public void TryParse_IncorrectJson_CarriesParserMessage()
    {
        var error = Assert.IsType<ValidationError.IncorrectJson>(Assert.Single(_sut.TryParse("[1, 2,]").Errors));

        Assert.Equal("incorrect-json", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_EmptyOrNullContent_FailsWithContentIsEmpty(string content)
    {
        var result = _sut.TryParse(content);

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.ContentIsEmpty>(Assert.Single(result.Errors));
    }

    [Fact]
    public void TryParse_NullContent_FailsWithContentIsEmpty()
    {
        var result = _sut.TryParse(null!);

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.ContentIsEmpty>(Assert.Single(result.Errors));
    }

    [Fact]
    public void TryParse_ElementsThatAreNotObjects_AreRejectedWithTheirIndex()
    {
        var result = _sut.TryParse("[1, 2]");

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors.OfType<ValidationError.JsonElementIsNotAnObject>(),
            e => Assert.Equal(0, e.Index),
            e => Assert.Equal(1, e.Index));
    }

    [Fact]
    public void TryParse_NestedValues_AreRejected()
    {
        var result = _sut.TryParse("""[{"id":1,"tags":["a","b"]}]""");

        var error = Assert.IsType<ValidationError.JsonValueIsNested>(Assert.Single(result.Errors));
        Assert.Equal(0, error.Index);
        Assert.Equal("tags", error.PropertyName);
    }

    [Fact]
    public void TryParse_RecordsWithDifferentFields_AreRejected()
    {
        var result = _sut.TryParse("""[{"id":1},{"name":"Anna"}]""");

        var error = Assert.IsType<ValidationError.JsonKeysAreNotUniform>(Assert.Single(result.Errors));
        Assert.Equal(1, error.Index);
    }

    [Fact]
    public void TryParse_EmptyPropertyName_IsRejected()
    {
        var result = _sut.TryParse("""[{"":1}]""");

        Assert.Contains(result.Errors, e => e is ValidationError.JsonPropertyNameIsEmpty);
    }

    [Fact]
    public void TryParse_MoreRecordsThanAllowed_FailsWithTooManyRecords()
    {
        var result = CreateSut(maxRecords: 2).TryParse("""[{"id":1},{"id":2},{"id":3}]""");

        var error = Assert.IsType<TooLargeError.TooManyRecords>(Assert.Single(result.Errors));
        Assert.Equal(3, error.Count);
        Assert.Equal(2, error.MaxRecords);
    }

    [Fact]
    public void TryParse_AggregatesEveryViolation_InOneResponse()
    {
        var result = _sut.TryParse("""[{"id":1,"tags":["a"]},{"other":2}]""");

        Assert.Contains(result.Errors, e => e is ValidationError.JsonValueIsNested);
        Assert.Contains(result.Errors, e => e is ValidationError.JsonKeysAreNotUniform);
    }
}
