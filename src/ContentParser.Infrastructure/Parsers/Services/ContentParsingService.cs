using System.Collections.Frozen;

using ContentParser.Parser.Models;
using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

using Microsoft.Extensions.Logging;

namespace ContentParser.Parser.Parsers.Services;

public sealed class ContentParsingService
{
    private readonly FrozenDictionary<string, IContentParser> _parsers;
    private readonly string[] _supportedTypes;
    private readonly Base64Decoder _base64Decoder;
    private readonly ILogger<ContentParsingService> _logger;

    public ContentParsingService(
        IEnumerable<IContentParser> parsers,
        Base64Decoder base64Decoder,
        ILogger<ContentParsingService> logger)
    {
        ArgumentNullException.ThrowIfNull(parsers);

        _parsers = parsers.ToFrozenDictionary(parser => parser.Type, StringComparer.OrdinalIgnoreCase);
        _supportedTypes = [.. _parsers.Keys.Order(StringComparer.Ordinal)];
        _base64Decoder = base64Decoder ?? throw new ArgumentNullException(nameof(base64Decoder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<string> SupportedTypes => _supportedTypes;

    public Result<ParseResult> ParseContent(string? type, string? content)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Fail(new RequestError.TypeIsMissing());
        }

        if (!_parsers.TryGetValue(type, out var parser))
        {
            return Fail(new RequestError.UnsupportedParser(type, _supportedTypes));
        }

        var result = _base64Decoder.TryDecode(content).Bind(parser.TryParse);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Parsing content of type {ContentType} failed: {ErrorCodes}",
                parser.Type,
                string.Join(", ", result.Errors.Select(error => error.Code)));
        }

        return result;
    }

    private Result<ParseResult> Fail(Error error)
    {
        _logger.LogWarning("Rejected request: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
        return Result<ParseResult>.Fail(error);
    }
}
