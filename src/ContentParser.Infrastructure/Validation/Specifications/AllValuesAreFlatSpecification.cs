using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>
/// Wartosci musza byc skalarne. Zagniezdzony obiekt lub tablica nie ma odpowiednika
/// w CSV, wiec nie zmiescilby sie w ujednoliconej strukturze wyjsciowej.
/// </summary>
public sealed class AllValuesAreFlatSpecification : Specification<JsonNode>
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
            if (array[i] is not JsonObject element)
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
