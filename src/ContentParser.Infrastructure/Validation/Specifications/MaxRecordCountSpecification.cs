using System.Text.Json.Nodes;

using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

namespace ContentParser.Parser.Validation.Specifications;

/// <summary>Gorny limit liczby rekordow — ochrona przed zadaniem, ktore przejdzie limit bajtow.</summary>
public sealed class MaxRecordCountSpecification : Specification<JsonNode>
{
    private readonly int _maxRecords;

    public MaxRecordCountSpecification(int maxRecords)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRecords);
        _maxRecords = maxRecords;
    }

    public override Result IsSatisfiedBy(JsonNode entity)
    {
        if (entity is not JsonArray array || array.Count <= _maxRecords)
        {
            return Result.Ok();
        }

        return Result.Fail(new TooLargeError.TooManyRecords(array.Count, _maxRecords));
    }
}
