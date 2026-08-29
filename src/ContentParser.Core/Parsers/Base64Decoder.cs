using System.Buffers;
using System.Buffers.Text;
using System.Text;

using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Core.Parsers;

public sealed class Base64Decoder
{
    private const int MaxBase64Padding = 2;

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
