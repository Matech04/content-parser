using System.Buffers;
using System.Buffers.Text;
using System.Text;

using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Parser.Parsers;

public sealed class Base64Decoder
{
    // Base64 dopelnia wejscie maksymalnie dwoma znakami '=', wiec gorne oszacowanie
    // dlugosci po zdekodowaniu zawyza najwyzej o tyle bajtow.
    private const int MaxBase64Padding = 2;

    // Scisle UTF-8: nieprawidlowe bajty maja byc bledem, a nie cichym U+FFFD.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly ParsingOptions _options;

    public Base64Decoder(IOptions<ParsingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public Result<string> TryDecode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result<string>.Fail(new ValidationError.ContentIsEmpty());
        }

        // Tani pre-filtr: odrzuca oczywiscie za duze wejscie, zanim zaalokujemy bufor.
        // Nie zastepuje dokladnego sprawdzenia nizej, bo to tylko gorne oszacowanie.
        var maxDecodedLength = Base64.GetMaxDecodedFromUtf8Length(input.Length);
        if (maxDecodedLength > _options.MaxDecodedContentBytes + MaxBase64Padding)
        {
            return Result<string>.Fail(new TooLargeError.ContentIsTooLarge(_options.MaxDecodedContentBytes));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(maxDecodedLength);
        try
        {
            if (!Convert.TryFromBase64String(input, buffer, out var bytesWritten))
            {
                return Result<string>.Fail(new ValidationError.IncorrectContentBase64Encoding());
            }

            // Dokladny limit — dopiero tutaj znamy rzeczywisty rozmiar tresci.
            if (bytesWritten > _options.MaxDecodedContentBytes)
            {
                return Result<string>.Fail(new TooLargeError.ContentIsTooLarge(_options.MaxDecodedContentBytes));
            }

            var span = TrimBom(buffer.AsSpan(0, bytesWritten));

            try
            {
                return Result<string>.Ok(StrictUtf8.GetString(span));
            }
            catch (DecoderFallbackException)
            {
                return Result<string>.Fail(new ValidationError.ContentIsNotValidUtf8());
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static ReadOnlySpan<byte> TrimBom(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith<byte>([0xEF, 0xBB, 0xBF]) ? bytes[3..] : bytes;
}
