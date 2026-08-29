using System.Text.Json.Nodes;

using ContentParser.Core.Validation.Specifications;

namespace ContentParser.Core.Validation.Builders;

public sealed class JsonValidatorBuilder
{
    private readonly List<ConfiguredRule> _rules = [];

    public JsonValidatorBuilder EnsureIsArray() =>
        Add(typeof(IsJsonArraySpecification), new IsJsonArraySpecification());

    public JsonValidatorBuilder EnsureAllElementsAreObjects() =>
        AddArrayRule(new AllElementsAreObjectsSpecification());

    public JsonValidatorBuilder EnsureAllValuesAreFlat() =>
        AddArrayRule(new AllValuesAreFlatSpecification());

    public JsonValidatorBuilder EnsurePropertyNamesAreNotEmpty() =>
        AddArrayRule(new PropertyNamesAreNotEmptySpecification());

    public JsonValidatorBuilder EnsureKeysAreUniform() =>
        AddArrayRule(new UniformKeysSpecification());

    public JsonValidatorBuilder EnsureAtMostRecords(int maxRecords) =>
        AddArrayRule(new MaxRecordCountSpecification(maxRecords));

    public Specification<JsonNode> Build()
    {
        if (_rules.Count == 0)
        {
            throw new InvalidOperationException("No specification was configured.");
        }

        return _rules
            .Select(rule => rule.Specification)
            .Aggregate(static (left, right) => left.And(right));
    }

    private JsonValidatorBuilder AddArrayRule(Specification<JsonArray> rule) =>
        Add(rule.GetType(), new WhenArraySpecification(rule));

    private JsonValidatorBuilder Add(Type key, Specification<JsonNode> specification)
    {
        var configured = new ConfiguredRule(key, specification);
        var existing = _rules.FindIndex(rule => rule.Key == key);

        if (existing >= 0)
        {
            _rules[existing] = configured;
        }
        else
        {
            _rules.Add(configured);
        }

        return this;
    }

    private readonly record struct ConfiguredRule(Type Key, Specification<JsonNode> Specification);
}
