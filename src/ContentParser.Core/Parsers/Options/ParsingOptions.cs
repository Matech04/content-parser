namespace ContentParser.Core.Parsers.Options;

public sealed class ParsingOptions
{
    public const string SectionName = "Parsing";

    public int MaxDecodedContentBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxRequestBodyBytes { get; set; } = 8 * 1024 * 1024;

    public int MaxRecords { get; set; } = 100_000;
}
