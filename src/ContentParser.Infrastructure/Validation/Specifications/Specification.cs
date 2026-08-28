using ContentParser.Parser.Results;

namespace ContentParser.Parser.Validation.Specifications;

public abstract class Specification<T>
{
    public abstract Result IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AndSpecification<T>(this, other);
    }
}

/// <summary>
/// Celowo NIE skraca obliczen: uzytkownik ma dostac komplet bledow w jednej odpowiedzi,
/// zamiast odbijac sie od API po jednym.
/// </summary>
public sealed class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        _left = left;
        _right = right;
    }

    public override Result IsSatisfiedBy(T entity)
    {
        var left = _left.IsSatisfiedBy(entity);
        var right = _right.IsSatisfiedBy(entity);

        if (left.IsSuccess && right.IsSuccess)
        {
            return Result.Ok();
        }

        return Result.FromErrors([.. left.Errors, .. right.Errors]);
    }
}
