using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>Pusta nazwa pola dalaby kolumne bez naglowka — odpowiednik pustego naglowka w CSV.</summary>
public sealed class PropertyNamesAreNotEmptySpecification : Specification<JsonNode>
{
    public override Result IsSatisfiedBy(JsonNode entity)
    {
        if (entity is not JsonArray array)
        {
            return Result.Ok();
        }

        List<Error> errors = [];

        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is JsonObject element && element.Any(p => string.IsNullOrWhiteSpace(p.Key)))
            {
                errors.Add(new ValidationError.JsonPropertyNameIsEmpty(i));
            }
        }

        return errors.Count == 0 ? Result.Ok() : Result.FromErrors(errors);
    }
}
