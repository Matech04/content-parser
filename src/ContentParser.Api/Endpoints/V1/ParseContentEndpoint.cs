using ContentParser.Api.Contracts.V1;

using ContentParser.Core.Parsers.Services;

namespace ContentParser.Api.Endpoints.V1;

internal static class ParseContentEndpoint
{
    internal static RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("/parse-content", Handle)
            .WithName("ParseContent")
            .WithSummary("Dekoduje ladunek Base64 i parsuje go do ujednoliconej struktury.")
            .Accepts<ParseContentRequestDto>("application/json")
            .Produces<ParseContentResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status415UnsupportedMediaType);

    internal static IResult Handle(ParseContentRequestDto request, ContentParsingService parsingService) =>
        parsingService
            .ParseContent(request.Type, request.Content)
            .Map(ParseContentResponseDto.From)
            .ToHttpResult();
}
