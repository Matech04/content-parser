using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>Korzeniem dokumentu musi byc tablica — rekordy sa z natury kolekcja.</summary>
public sealed class IsJsonArraySpecification : Specification<JsonNode>
{
    public override Result IsSatisfiedBy(JsonNode entity) =>
        entity is JsonArray ? Result.Ok() : Result.Fail(new ValidationError.JsonIsNotAnArray());
}
