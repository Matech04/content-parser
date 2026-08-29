using System.Text.Json.Nodes;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Validation.Specifications;

public sealed class MaxRecordCountSpecification : Specification<JsonArray>
{
    private readonly int _maxRecords;

    public MaxRecordCountSpecification(int maxRecords)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRecords);
        _maxRecords = maxRecords;
    }

    public override Result IsSatisfiedBy(JsonArray candidate) =>
        candidate.Count <= _maxRecords
            ? Result.Ok()
            : Result.Fail(new TooLargeError.TooManyRecords(candidate.Count, _maxRecords));
}
