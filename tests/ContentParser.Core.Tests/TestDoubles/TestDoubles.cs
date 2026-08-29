using ContentParser.Core.Models;
using ContentParser.Core.Parsers;
using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;
using ContentParser.Core.Validation.Specifications;

namespace ContentParser.Core.Tests.TestDoubles;

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

internal sealed class CountingSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;

    public CountingSpecification(Specification<T> inner) => _inner = inner;

    public int Evaluations { get; private set; }

    public override Result IsSatisfiedBy(T entity)
    {
        Evaluations++;
        return _inner.IsSatisfiedBy(entity);
    }
}

internal sealed class StubContentParser : IContentParser
{
    private readonly Func<string, Result<ParseResult>> _onParse;

    public StubContentParser(string type, Func<string, Result<ParseResult>>? onParse = null)
    {
        Type = type;
        _onParse = onParse ?? (_ => Result<ParseResult>.Ok(new ParseResult(0, [])));
    }

    public string Type { get; }

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
