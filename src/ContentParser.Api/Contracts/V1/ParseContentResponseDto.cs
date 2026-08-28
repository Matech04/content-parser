using ContentParser.Parser.Models;

namespace ContentParser.Api.Contracts.V1;

/// <summary>
/// Ujednolicona odpowiedz — identyczna dla CSV i INTERNAL_JSON.
/// </summary>
public sealed record ParseContentResponseDto(
    string Status,
    int ProcessedCount,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Data)
{
    public static ParseContentResponseDto From(ParseResult result) =>
        new("Success", result.ProcessedCount, [.. result.Records.Select(record => record.Fields)]);
}
