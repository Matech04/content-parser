using System.Text.Json.Nodes;

using ContentParser.Core.Results.Errors;
using ContentParser.Core.Validation.Builders;

namespace ContentParser.Core.Tests.Validation;

public class JsonValidatorBuilderTests
{
    [Fact]
    public void Build_WithoutAnySpecification_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new JsonValidatorBuilder().Build());
    }

    [Fact]
    public void EveryStep_ReturnsSameBuilder_ForFluentChaining()
    {
        var builder = new JsonValidatorBuilder();

        Assert.Same(builder, builder.EnsureIsArray());
        Assert.Same(builder, builder.EnsureAllElementsAreObjects());
        Assert.Same(builder, builder.EnsureAllValuesAreFlat());
        Assert.Same(builder, builder.EnsurePropertyNamesAreNotEmpty());
        Assert.Same(builder, builder.EnsureKeysAreUniform());
        Assert.Same(builder, builder.EnsureAtMostRecords(10));
    }

    [Fact]
    public void EnsureIsArray_AcceptsArrays()
    {
        var spec = new JsonValidatorBuilder().EnsureIsArray().Build();

        Assert.True(spec.IsSatisfiedBy(new JsonArray()).IsSuccess);
    }

    [Fact]
    public void EnsureIsArray_RejectsObjects()
    {
        var spec = new JsonValidatorBuilder().EnsureIsArray().Build();

        Assert.IsType<ValidationError.JsonIsNotAnArray>(Assert.Single(spec.IsSatisfiedBy(new JsonObject()).Errors));
    }

    [Fact]
    public void SameStepTwice_DoesNotDuplicateErrors()
    {
        var spec = new JsonValidatorBuilder().EnsureIsArray().EnsureIsArray().Build();

        Assert.Single(spec.IsSatisfiedBy(new JsonObject()).Errors);
    }

    [Fact]
    public void EnsureAtMostRecords_CalledTwice_KeepsTheLastLimit()
    {
        var spec = new JsonValidatorBuilder().EnsureAtMostRecords(10).EnsureAtMostRecords(1).Build();

        Assert.IsType<TooLargeError.TooManyRecords>(
            Assert.Single(spec.IsSatisfiedBy(JsonNode.Parse("[1,2]")!).Errors));
    }

    [Fact]
    public void CombinedSpecifications_AggregateEveryViolation()
    {
        var spec = new JsonValidatorBuilder()
            .EnsureAllElementsAreObjects()
            .EnsureKeysAreUniform()
            .Build();

        var result = spec.IsSatisfiedBy(JsonNode.Parse("""[{"a":1}, 2, {"b":3}]""")!);

        Assert.Contains(result.Errors, e => e is ValidationError.JsonElementIsNotAnObject);
        Assert.Contains(result.Errors, e => e is ValidationError.JsonKeysAreNotUniform);
    }
}
