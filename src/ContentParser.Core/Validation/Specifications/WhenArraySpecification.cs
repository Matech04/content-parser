using System.Text.Json.Nodes;

using ContentParser.Core.Results;

namespace ContentParser.Core.Validation.Specifications;

public sealed class WhenArraySpecification : Specification<JsonNode>
{
    private readonly Specification<JsonArray> _rule;

    public WhenArraySpecification(Specification<JsonArray> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rule = rule;
    }

    public override Result IsSatisfiedBy(JsonNode candidate) =>
        candidate is JsonArray array ? _rule.IsSatisfiedBy(array) : Result.Ok();
}
