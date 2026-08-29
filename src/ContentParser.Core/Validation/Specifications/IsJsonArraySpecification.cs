using System.Text.Json.Nodes;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class IsJsonArraySpecification : Specification<JsonNode>
{
    public override Result IsSatisfiedBy(JsonNode candidate) =>
        candidate is JsonArray ? Result.Ok() : Result.Fail(new ValidationError.JsonIsNotAnArray());
}
