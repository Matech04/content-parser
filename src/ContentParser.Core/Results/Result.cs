using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Results;

public sealed class Result<T>
{
    private readonly T _value;

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    private Result(T value)
    {
        _value = value;
        Errors = [];
    }

    private Result(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        _value = default!;
        Errors = [.. errors];
    }

    public static Result<T> Ok(T value) => new(value);

    public static Result<T> Fail(Error error, params Error[] additional)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([error, .. additional]);
    }

    internal static Result<T> FromErrors(IReadOnlyList<Error> errors) => new(errors);

    public bool TryGetValue(out T value)
    {
        value = IsSuccess ? _value : default!;
        return IsSuccess;
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value) : onFailure(Errors);
    }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next(_value) : Result<TOut>.FromErrors(Errors);
    }

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Result<TOut>.Ok(mapper(_value)) : Result<TOut>.FromErrors(Errors);
    }

    public override string ToString() => IsSuccess
        ? $"Success<{typeof(T).Name}>"
        : $"Failure<{typeof(T).Name}>({string.Join(", ", Errors.Select(e => e.Code))})";
}

public sealed class Result
{
    private static readonly Result SuccessInstance = new();

    public IReadOnlyList<Error> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    private Result() => Errors = [];

    private Result(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = [.. errors];
    }

    public static Result Ok() => SuccessInstance;

    public static Result Fail(Error error, params Error[] additional)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([error, .. additional]);
    }

    internal static Result FromErrors(IReadOnlyList<Error> errors) => new(errors);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    public Result<TOut> Bind<TOut>(Func<Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : Result<TOut>.FromErrors(Errors);
    }

    public override string ToString() => IsSuccess
        ? "Success"
        : $"Failure({string.Join(", ", Errors.Select(e => e.Code))})";
}
