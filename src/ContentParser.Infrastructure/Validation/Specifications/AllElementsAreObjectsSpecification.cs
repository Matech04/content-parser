using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>
/// Kazdy element tablicy musi byc obiektem — inaczej nie da sie go odwzorowac
/// na rekord (nazwa pola -> wartosc) wspolny z CSV.
/// </summary>
public sealed class AllElementsAreObjectsSpecification : Specification<JsonNode>
{
    public override Result IsSatisfiedBy(JsonNode entity)
    {
        if (entity is not JsonArray array)
        {
            return Result.Ok();  // pilnuje tego IsJsonArraySpecification
        }

        List<Error> errors = [];

        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is not JsonObject)
            {
                errors.Add(new ValidationError.JsonElementIsNotAnObject(i));
            }
        }

        return errors.Count == 0 ? Result.Ok() : Result.FromErrors(errors);
    }
}
