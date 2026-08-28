namespace ContentParser.Parser.Models;

/// <summary>
/// Ujednolicona reprezentacja sparsowanej tresci — wspolna dla CSV i INTERNAL_JSON.
/// Rekord to plaska mapa nazwa pola -> wartosc; brak wartosci reprezentuje null.
/// </summary>
public sealed record ParseResult(int ProcessedCount, IReadOnlyList<ParsedRecord> Records);

/// <summary>Pojedynczy rekord. Kolejnosc pol jest zachowana (kolejnosc kolumn / wlasciwosci).</summary>
public sealed record ParsedRecord(IReadOnlyDictionary<string, string?> Fields);
