namespace ContentParser.Parser.Parsers.Options;

public sealed class ParsingOptions
{
    public const string SectionName = "Parsing";

    /// <summary>Limit na tresc PO zdekodowaniu Base64.</summary>
    public int MaxDecodedContentBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Limit na cale cialo zadania HTTP. Musi byc wiekszy od <see cref="MaxDecodedContentBytes"/>,
    /// bo Base64 zwieksza rozmiar o ~33%, a do tego dochodzi koperta JSON.
    /// </summary>
    public int MaxRequestBodyBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>Maksymalna liczba rekordow (wierszy CSV / elementow tablicy JSON).</summary>
    public int MaxRecords { get; set; } = 100_000;
}
