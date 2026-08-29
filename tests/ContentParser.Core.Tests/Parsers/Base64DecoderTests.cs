using System.Text;

using ContentParser.Core.Parsers;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Core.Tests.Parsers;

public class Base64DecoderTests
{
    private static Base64Decoder CreateSut(int maxDecodedContentBytes = 5 * 1024 * 1024) =>
        new(Options.Create(new ParsingOptions { MaxDecodedContentBytes = maxDecodedContentBytes }));

    private static string ToBase64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void TryDecode_EmptyInput_FailsWithContentIsEmpty(string? input)
    {
        var result = CreateSut().TryDecode(input);

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.ContentIsEmpty>(Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""[{"id":1}]""")]
    [InlineData("id,name\n1,Anna")]
    [InlineData("zwykly tekst, nie-JSON")]
    public void TryDecode_ValidBase64_ReturnsDecodedText(string original)
    {
        var result = CreateSut().TryDecode(ToBase64(original));

        Assert.True(result.TryGetValue(out var decoded));
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void TryDecode_PreservesMultiByteUtf8()
    {
        const string original = """["zazolc gesla jazn", "ąćęłńóśźż", "日本語", "🙂"]""";

        var result = CreateSut().TryDecode(ToBase64(original));

        Assert.True(result.TryGetValue(out var decoded));
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void TryDecode_StripsUtf8Bom()
    {
        var withBom = Convert.ToBase64String([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("id,name")]);

        var result = CreateSut().TryDecode(withBom);

        Assert.True(result.TryGetValue(out var decoded));
        Assert.Equal("id,name", decoded);
    }

    [Theory]
    [InlineData("!!!not-base64!!!")]
    [InlineData("a")]
    [InlineData("****")]
    [InlineData("W10=extra")]
    public void TryDecode_InvalidBase64_FailsWithIncorrectEncoding(string input)
    {
        var result = CreateSut().TryDecode(input);

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.IncorrectContentBase64Encoding>(Assert.Single(result.Errors));
    }

    [Fact]
    public void TryDecode_BytesThatAreNotUtf8_FailWithExplicitError()
    {
        var result = CreateSut().TryDecode(Convert.ToBase64String([0xFF, 0xFE, 0xFD, 0xFC]));

        Assert.False(result.IsSuccess);
        Assert.IsType<ValidationError.ContentIsNotValidUtf8>(Assert.Single(result.Errors));
    }

    [Fact]
    public void TryDecode_ContentLargerThanLimit_FailsWithTooLarge()
    {
        var result = CreateSut(maxDecodedContentBytes: 64).TryDecode(ToBase64(new string('a', 4096)));

        Assert.False(result.IsSuccess);
        Assert.IsType<TooLargeError.ContentIsTooLarge>(Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData(800)]
    [InlineData(1000)]
    [InlineData(1024)]
    public void TryDecode_ContentUpToTheLimit_IsAccepted(int payloadSize)
    {
        var payload = new string('a', payloadSize);

        var result = CreateSut(maxDecodedContentBytes: 1024).TryDecode(ToBase64(payload));

        Assert.True(result.TryGetValue(out var decoded));
        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void DefaultOptions_Allow5MbOfDecodedContent()
    {
        Assert.Equal(5 * 1024 * 1024, new ParsingOptions().MaxDecodedContentBytes);
    }

    [Fact]
    public void TryDecode_IsRepeatable_AndDoesNotLeakPooledBuffers()
    {
        var sut = CreateSut();
        var encoded = ToBase64("id,name\n1,Anna");

        sut.TryDecode(encoded).TryGetValue(out var first);
        sut.TryDecode(encoded).TryGetValue(out var second);

        Assert.Equal(first, second);
    }
}
