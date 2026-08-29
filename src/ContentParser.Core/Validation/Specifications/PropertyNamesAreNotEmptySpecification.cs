using System.Text.Json.Nodes;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class PropertyNamesAreNotEmptySpecification : Specification<JsonArray>
{
    public override Result IsSatisfiedBy(JsonArray candidate)
    {
        List<Error> errors = [];

        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i] is JsonObject element && element.Any(p => string.IsNullOrWhiteSpace(p.Key)))
            {
                errors.Add(new ValidationError.JsonPropertyNameIsEmpty(i));
            }
        }

        return errors.Count == 0 ? Result.Ok() : Result.FromErrors(errors);
    }
}
