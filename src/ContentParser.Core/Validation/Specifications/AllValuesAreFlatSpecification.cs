using System.Text.Json.Nodes;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class AllValuesAreFlatSpecification : Specification<JsonArray>
{
    public override Result IsSatisfiedBy(JsonArray candidate)
    {
        List<Error> errors = [];

        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i] is not JsonObject element)
            {
                continue;
            }

            foreach (var (name, value) in element)
            {
                if (value is JsonObject or JsonArray)
                {
                    errors.Add(new ValidationError.JsonValueIsNested(i, name));
                }
            }
        }

        return errors.Count == 0 ? Result.Ok() : Result.FromErrors(errors);
    }
}
