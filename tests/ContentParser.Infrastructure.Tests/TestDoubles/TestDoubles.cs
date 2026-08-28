using ContentParser.Parser.Models;
using ContentParser.Parser.Parsers;
using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;
using ContentParser.Parser.Validation.Specifications;

namespace ContentParser.Infrastructure.Tests.TestDoubles;

internal sealed record TestError(string Code, string Message) : Error(Code, Message);

internal sealed class AlwaysTrueSpecification<T> : Specification<T>
{
    public override Result IsSatisfiedBy(T entity) => Result.Ok();
}

internal sealed class AlwaysFalseSpecification<T> : Specification<T>
{
    private readonly Error _error;

    public AlwaysFalseSpecification(string code) => _error = new TestError(code, code);

    public override Result IsSatisfiedBy(T entity) => Result.Fail(_error);
}

/// <summary>Pozwala testowac ContentParsingService w oderwaniu od konkretnego parsera.</summary>
internal sealed class StubContentParser : IContentParser
{
    private readonly Func<string, Result<ParseResult>> _onParse;

    public StubContentParser(string type, Func<string, Result<ParseResult>>? onParse = null)
    {
        Type = type;
        _onParse = onParse ?? (_ => Result<ParseResult>.Ok(new ParseResult(0, [])));
    }

    public string Type { get; }

    /// <summary>Tresc, ktora ostatnio trafila do parsera (juz po zdekodowaniu Base64).</summary>
    public string? LastContent { get; private set; }

    public int CallCount { get; private set; }

    public Result<ParseResult> TryParse(string content)
    {
        LastContent = content;
        CallCount++;
        return _onParse(content);
    }

    public static StubContentParser Failing(string type, Error error) =>
        new(type, _ => Result<ParseResult>.Fail(error));
}
