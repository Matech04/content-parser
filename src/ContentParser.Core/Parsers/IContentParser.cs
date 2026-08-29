using ContentParser.Core.Models;
using ContentParser.Core.Results;

namespace ContentParser.Core.Parsers;

public interface IContentParser
{
    string Type { get; }

    Result<ParseResult> TryParse(string content);
}
