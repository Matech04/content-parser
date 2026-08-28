using ContentParser.Parser.Models;
using ContentParser.Parser.Results;

namespace ContentParser.Parser.Parsers;

public interface IContentParser
{
    /// <summary>Wartosc pola "type" w zadaniu, ktora obsluguje ten parser.</summary>
    string Type { get; }

    Result<ParseResult> TryParse(string content);
}
