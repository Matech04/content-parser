using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>
/// Wszystkie rekordy musza miec ten sam zestaw pol. To odpowiednik staloscii kolumn
/// w CSV i warunek tego, by obie sciezki dawaly te sama strukture wyjsciowa.
/// Wzorcem jest pierwszy element.
/// </summary>
public sealed class UniformKeysSpecification : Specification<JsonNode>
{
    public override Result IsSatisfiedBy(JsonNode entity)
    {
        if (entity is not JsonArray array || array.Count == 0)
        {
            return Result.Ok();
        }

        if (array[0] is not JsonObject first)
        {
            return Result.Ok();  // pilnuje tego AllElementsAreObjectsSpecification
        }

        string[] expected = [.. first.Select(p => p.Key)];
        List<Error> errors = [];

        for (var i = 1; i < array.Count; i++)
        {
            if (array[i] is not JsonObject element)
            {
                continue;
            }

            string[] actual = [.. element.Select(p => p.Key)];

            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                errors.Add(new ValidationError.JsonKeysAreNotUniform(i, expected, actual));
            }
        }

        return errors.Count == 0 ? Result.Ok() : Result.FromErrors(errors);
    }
}
