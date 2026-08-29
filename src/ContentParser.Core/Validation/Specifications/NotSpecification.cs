using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;
    private readonly Error _whenSatisfied;

    public NotSpecification(Specification<T> inner, Error whenSatisfied)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(whenSatisfied);
        _inner = inner;
        _whenSatisfied = whenSatisfied;
    }

    public override Result IsSatisfiedBy(T candidate) =>
        _inner.IsSatisfiedBy(candidate).IsSuccess
            ? Result.Fail(_whenSatisfied)
            : Result.Ok();
}
