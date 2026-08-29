using ContentParser.Core.Results;

namespace ContentParser.Core.Validation.Specifications;

public sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        _left = left;
        _right = right;
    }

    public override Result IsSatisfiedBy(T candidate)
    {
        var left = _left.IsSatisfiedBy(candidate);
        if (left.IsSuccess)
        {
            return Result.Ok();
        }

        var right = _right.IsSatisfiedBy(candidate);

        return right.IsSuccess
            ? Result.Ok()
            : Result.FromErrors([.. left.Errors, .. right.Errors]);
    }
}
