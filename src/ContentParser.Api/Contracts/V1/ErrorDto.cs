namespace ContentParser.Api.Contracts.V1;

/// <summary>Pojedynczy blad w kolekcji `errors` odpowiedzi ProblemDetails.</summary>
public sealed record ErrorDto(string Code, string Message);
