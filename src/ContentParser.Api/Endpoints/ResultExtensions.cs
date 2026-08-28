using ContentParser.Api.Contracts.V1;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Api.Endpoints;

/// <summary>
/// Granica miedzy swiatem <see cref="Result"/> a HTTP. Jedyne miejsce w aplikacji,
/// w ktorym porazka przestaje byc danymi i staje sie odpowiedzia.
/// </summary>
public static class ResultExtensions
{
    private const string ErrorTypePrefix = "urn:content-parser:error:";

    public static IResult ToHttpResult(this Result result) =>
        result.Match(
            onSuccess: () => Results.Ok(),
            onFailure: MapErrors);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: MapErrors);

    /// <summary>
    /// Status wyznacza pierwszy blad; pelna lista trafia do rozszerzenia `errors`,
    /// zeby agregacja bledow ze specyfikacji nie ginela na granicy HTTP.
    /// </summary>
    private static IResult MapErrors(IReadOnlyList<Error> errors)
    {
        var first = errors[0];

        var (statusCode, title) = first switch
        {
            TooLargeError => (StatusCodes.Status413PayloadTooLarge, "Content Too Large"),
            ValidationError => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            RequestError => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request"),
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: first.Message,
            type: ErrorTypePrefix + first.Code,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = errors.Select(error => new ErrorDto(error.Code, error.Message)).ToArray(),
            });
    }
}
