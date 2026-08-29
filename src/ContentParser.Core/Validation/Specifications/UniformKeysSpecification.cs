using System.Text.Json.Nodes;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class UniformKeysSpecification : Specification<JsonArray>
{
    public override Result IsSatisfiedBy(JsonArray candidate)
    {
        if (candidate.Count == 0 || candidate[0] is not JsonObject first)
        {
            return Result.Ok();
        }

        string[] expected = [.. first.Select(p => p.Key)];
        List<Error> errors = [];

        for (var i = 1; i < candidate.Count; i++)
        {
            if (candidate[i] is not JsonObject element)
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
