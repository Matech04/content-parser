using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public abstract class Specification<T>
{
    public abstract Result IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);

    public Specification<T> Not(Error whenSatisfied) => new NotSpecification<T>(this, whenSatisfied);
}
