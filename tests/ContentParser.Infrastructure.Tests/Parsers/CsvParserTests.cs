using ContentParser.Parser.Parsers;
using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Infrastructure.Tests.Parsers;

public class CsvParserTests
{
    private static CsvParser CreateSut(int maxRecords = 100_000) =>
        new(Options.Create(new ParsingOptions { MaxRecords = maxRecords }));

    private readonly CsvParser _sut = CreateSut();

    [Fact]
    public void Type_IsCsv()
    {
        Assert.Equal("CSV", _sut.Type);
    }

    [Fact]
    public void TryParse_HeaderAndRows_MapsColumnsToFields()
    {
        var result = _sut.TryParse("id,name,city\n1,Anna,Katowice\n2,Piotr,Gliwice");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(2, parsed.ProcessedCount);
        Assert.Equal("1", parsed.Records[0].Fields["id"]);
        Assert.Equal("Anna", parsed.Records[0].Fields["name"]);
        Assert.Equal("Gliwice", parsed.Records[1].Fields["city"]);
    }

    [Fact]
    public void TryParse_HeaderOnly_ReturnsZeroRecords()
    {
        var result = _sut.TryParse("id,name");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(0, parsed.ProcessedCount);
    }

    [Theory]
    [InlineData("id,name\r\n1,Anna\r\n2,Piotr")]   // CRLF
    [InlineData("id,name\n1,Anna\n2,Piotr")]       // LF
    [InlineData("id,name\r\n1,Anna\n2,Piotr")]     // mieszane
    public void TryParse_HandlesBothLineEndings(string csv)
    {
        var result = _sut.TryParse(csv);

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(2, parsed.ProcessedCount);
    }

    [Theory]
    [InlineData("id,name\n1,Anna\n")]
    [InlineData("id,name\n1,Anna\r\n")]
    [InlineData("id,name\n1,Anna\n\n\n")]
    public void TryParse_IgnoresTrailingAndBlankLines(string csv)
    {
        var result = _sut.TryParse(csv);

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(1, parsed.ProcessedCount);
    }

    [Fact]
    public void TryParse_QuotedField_MayContainDelimiter()
    {
        var result = _sut.TryParse("""
            id,name
            1,"Kowalski, Jan"
            """);

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal("Kowalski, Jan", parsed.Records[0].Fields["name"]);
    }

    [Fact]
    public void TryParse_QuotedField_MayContainNewline()
    {
        var result = _sut.TryParse("id,note\n1,\"pierwsza\ndruga\"");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(1, parsed.ProcessedCount);
        Assert.Equal("pierwsza\ndruga", parsed.Records[0].Fields["note"]);
    }

    [Fact]
    public void TryParse_DoubledQuote_IsUnescapedToSingleQuote()
    {
        var result = _sut.TryParse("""
            id,quote
            1,"powiedzial ""tak"" glosno"
            """);

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal("""powiedzial "tak" glosno""", parsed.Records[0].Fields["quote"]);
    }

    [Fact]
    public void TryParse_EmptyQuotedField_BecomesEmptyString()
    {
        var result = _sut.TryParse("id,name\n1,\"\"");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(string.Empty, parsed.Records[0].Fields["name"]);
    }

    [Fact]
    public void TryParse_TrailingEmptyField_IsPreserved()
    {
        var result = _sut.TryParse("id,name\n1,");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(string.Empty, parsed.Records[0].Fields["name"]);
    }

    [Fact]
    public void TryParse_HeaderNamesAreTrimmed_ButValuesAreNot()
    {
        var result = _sut.TryParse("  id  ,  name  \n1,  Anna  ");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal("  Anna  ", parsed.Records[0].Fields["name"]);
    }

    [Fact]
    public void TryParse_PreservesMultiByteUtf8()
    {
        var result = _sut.TryParse("name\nzazolc gesla jazn\nąćęłńóśźż\n日本語");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(3, parsed.ProcessedCount);
        Assert.Equal("日本語", parsed.Records[2].Fields["name"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_EmptyContent_FailsWithContentIsEmpty(string csv)
    {
        var result = _sut.TryParse(csv);

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
    public void TryParse_RowWithTooFewFields_ReportsRowNumber()
    {
        var result = _sut.TryParse("id,name,city\n1,Anna");

        var error = Assert.IsType<ValidationError.CsvRowHasWrongFieldCount>(Assert.Single(result.Errors));
        Assert.Equal(2, error.RowNumber);      // naglowek to wiersz 1
        Assert.Equal(3, error.Expected);
        Assert.Equal(2, error.Actual);
    }

    [Fact]
    public void TryParse_RowWithTooManyFields_IsRejected()
    {
        var result = _sut.TryParse("id,name\n1,Anna,extra");

        var error = Assert.IsType<ValidationError.CsvRowHasWrongFieldCount>(Assert.Single(result.Errors));
        Assert.Equal(3, error.Actual);
    }

    [Fact]
    public void TryParse_ReportsEveryMalformedRow_NotJustTheFirst()
    {
        var result = _sut.TryParse("id,name\n1\n2\n3,Anna,extra");

        Assert.Equal(3, result.Errors.OfType<ValidationError.CsvRowHasWrongFieldCount>().Count());
    }

    [Fact]
    public void TryParse_DuplicateColumn_IsRejected()
    {
        var result = _sut.TryParse("id,name,id\n1,Anna,2");

        var error = Assert.IsType<ValidationError.CsvDuplicateColumn>(Assert.Single(result.Errors));
        Assert.Equal("id", error.ColumnName);
    }

    [Fact]
    public void TryParse_EmptyColumnName_IsRejected()
    {
        var result = _sut.TryParse("id,,city\n1,x,y");

        var error = Assert.IsType<ValidationError.CsvColumnNameIsEmpty>(Assert.Single(result.Errors));
        Assert.Equal(1, error.ColumnIndex);
    }

    [Fact]
    public void TryParse_UnterminatedQuote_IsRejected()
    {
        var result = _sut.TryParse("id,name\n1,\"Anna");

        var error = Assert.IsType<ValidationError.CsvQuotedFieldNotClosed>(Assert.Single(result.Errors));
        Assert.Equal(2, error.RowNumber);
    }

    [Fact]
    public void TryParse_MoreRowsThanAllowed_FailsWithTooManyRecords()
    {
        var result = CreateSut(maxRecords: 2).TryParse("id\n1\n2\n3");

        var error = Assert.IsType<TooLargeError.TooManyRecords>(Assert.Single(result.Errors));
        Assert.Equal(3, error.Count);
        Assert.Equal(2, error.MaxRecords);
    }

    [Fact]
    public void TryParse_SingleColumn_IsSupported()
    {
        var result = _sut.TryParse("name\nAnna\nPiotr");

        Assert.True(result.TryGetValue(out var parsed));
        Assert.Equal(2, parsed.ProcessedCount);
        Assert.Equal("Piotr", parsed.Records[1].Fields["name"]);
    }
}
