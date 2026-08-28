using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Results;

/// <summary>
/// Wynik operacji, ktora moze zawiesc w sposob przewidywalny (zle dane wejsciowe).
/// Bledy programisty nadal sygnalizowane sa wyjatkami — patrz strażniki w konstruktorach.
/// </summary>
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

    /// <summary>
    /// Pierwszy blad jest osobnym parametrem, wiec "porazka bez bledu" jest niewyrazalna
    /// w typie — nie trzeba jej pilnowac w runtime.
    /// </summary>
    public static Result<T> Fail(Error error, params Error[] additional)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([error, .. additional]);
    }

    /// <summary>Przenoszenie bledow miedzy Result-ami; niepustosc gwarantuje wolajacy (!IsSuccess).</summary>
    internal static Result<T> FromErrors(IReadOnlyList<Error> errors) => new(errors);

    /// <summary>Bezpieczny odczyt wartosci — nigdy nie rzuca. Idiom jak Dictionary.TryGetValue.</summary>
    public bool TryGetValue(out T value)
    {
        value = IsSuccess ? _value : default!;
        return IsSuccess;
    }

    /// <summary>Wyjscie ze swiata Result: obie galezie musza dac ten sam TOut.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value) : onFailure(Errors);
    }

    /// <summary>Pozostanie w swiecie Result: nastepny krok, ktory tez moze zawiesc.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next(_value) : Result<TOut>.FromErrors(Errors);
    }

    /// <summary>Pozostanie w swiecie Result: przeksztalcenie, ktore nie moze zawiesc.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return IsSuccess ? Result<TOut>.Ok(mapper(_value)) : Result<TOut>.FromErrors(Errors);
    }

    // Wlasne ToString(): domyslne z `record` wypisalo by Value, ktore na porazce nie istnieje.
    public override string ToString() => IsSuccess
        ? $"Success<{typeof(T).Name}>"
        : $"Failure<{typeof(T).Name}>({string.Join(", ", Errors.Select(e => e.Code))})";
}

/// <summary>Wynik operacji bez wartosci — uzywany przez specyfikacje walidacyjne.</summary>
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

    /// <summary>Przenosi bledy do Result&lt;TOut&gt; albo wykonuje nastepny krok.</summary>
    public Result<TOut> Bind<TOut>(Func<Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next() : Result<TOut>.FromErrors(Errors);
    }

    public override string ToString() => IsSuccess
        ? "Success"
        : $"Failure({string.Join(", ", Errors.Select(e => e.Code))})";
}
