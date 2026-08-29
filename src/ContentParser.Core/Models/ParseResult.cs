namespace ContentParser.Core.Models;

public sealed record ParseResult(int ProcessedCount, IReadOnlyList<ParsedRecord> Records);

public sealed record ParsedRecord(IReadOnlyDictionary<string, string?> Fields);
