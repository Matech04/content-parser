using System.Text.Json.Nodes;

using ContentParser.Parser.Results.Errors;
using ContentParser.Parser.Validation.Specifications;

namespace ContentParser.Infrastructure.Tests.Validation;

public class IsJsonArraySpecificationTests
{
    private readonly IsJsonArraySpecification _sut = new();

    [Theory]
    [InlineData("[]")]
    [InlineData("""[{"id":1}]""")]
    public void Arrays_AreSatisfied(string json)
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse(json)!).IsSuccess);
    }

    [Theory]
    [InlineData("""{"id":1}""")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("true")]
    public void NonArray_FailsWithJsonIsNotAnArray(string json)
    {
        var result = _sut.IsSatisfiedBy(JsonNode.Parse(json)!);

        Assert.IsType<ValidationError.JsonIsNotAnArray>(Assert.Single(result.Errors));
    }
}

public class AllElementsAreObjectsSpecificationTests
{
    private readonly AllElementsAreObjectsSpecification _sut = new();

    [Fact]
    public void ArrayOfObjects_IsSatisfied()
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse("""[{"id":1},{"id":2}]""")!).IsSuccess);
    }

    [Fact]
    public void ReportsIndexOfEveryOffendingElement()
    {
        var result = _sut.IsSatisfiedBy(JsonNode.Parse("""[{"id":1}, 2, "x"]""")!);

        Assert.Collection(
            result.Errors.Cast<ValidationError.JsonElementIsNotAnObject>(),
            e => Assert.Equal(1, e.Index),
            e => Assert.Equal(2, e.Index));
    }

    [Fact]
    public void NonArrayRoot_IsLeftToIsJsonArraySpecification()
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse("""{"id":1}""")!).IsSuccess);
    }
}

public class AllValuesAreFlatSpecificationTests
{
    private readonly AllValuesAreFlatSpecification _sut = new();

    [Fact]
    public void ScalarValues_AreSatisfied()
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse("""[{"a":1,"b":"x","c":null,"d":true}]""")!).IsSuccess);
    }

    [Theory]
    [InlineData("""[{"a":{"nested":1}}]""")]
    [InlineData("""[{"a":[1,2]}]""")]
    public void NestedValues_AreRejected(string json)
    {
        var error = Assert.IsType<ValidationError.JsonValueIsNested>(
            Assert.Single(_sut.IsSatisfiedBy(JsonNode.Parse(json)!).Errors));

        Assert.Equal("a", error.PropertyName);
        Assert.Equal(0, error.Index);
    }

    [Fact]
    public void ReportsEveryNestedProperty()
    {
        var result = _sut.IsSatisfiedBy(JsonNode.Parse("""[{"a":[1],"b":{"x":1}}]""")!);

        Assert.Equal(2, result.Errors.Count);
    }
}

public class PropertyNamesAreNotEmptySpecificationTests
{
    private readonly PropertyNamesAreNotEmptySpecification _sut = new();

    [Fact]
    public void NamedProperties_AreSatisfied()
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse("""[{"id":1}]""")!).IsSuccess);
    }

    [Theory]
    [InlineData("""[{"":1}]""")]
    [InlineData("""[{"   ":1}]""")]
    public void EmptyName_IsRejected(string json)
    {
        var error = Assert.IsType<ValidationError.JsonPropertyNameIsEmpty>(
            Assert.Single(_sut.IsSatisfiedBy(JsonNode.Parse(json)!).Errors));

        Assert.Equal(0, error.Index);
    }
}

public class UniformKeysSpecificationTests
{
    private readonly UniformKeysSpecification _sut = new();

    [Theory]
    [InlineData("[]")]
    [InlineData("""[{"id":1}]""")]
    [InlineData("""[{"id":1,"name":"a"},{"id":2,"name":"b"}]""")]
    public void UniformRecords_AreSatisfied(string json)
    {
        Assert.True(_sut.IsSatisfiedBy(JsonNode.Parse(json)!).IsSuccess);
    }

    [Fact]
    public void DifferentKeys_AreRejected_WithBothSets()
    {
        var result = _sut.IsSatisfiedBy(JsonNode.Parse("""[{"id":1},{"name":"a"}]""")!);

        var error = Assert.IsType<ValidationError.JsonKeysAreNotUniform>(Assert.Single(result.Errors));
        Assert.Equal(1, error.Index);
        Assert.Equal(["id"], error.Expected);
        Assert.Equal(["name"], error.Actual);
    }

    [Fact]
    public void DifferentOrderOfKeys_IsRejected()
    {
        // Kolejnosc pol odpowiada kolejnosci kolumn w CSV, wiec musi byc stala.
        var result = _sut.IsSatisfiedBy(JsonNode.Parse("""[{"a":1,"b":2},{"b":2,"a":1}]""")!);

        Assert.IsType<ValidationError.JsonKeysAreNotUniform>(Assert.Single(result.Errors));
    }

    [Fact]
    public void ReportsEveryDivergentRecord()
    {
        var result = _sut.IsSatisfiedBy(JsonNode.Parse("""[{"a":1},{"b":2},{"c":3}]""")!);

        Assert.Equal(2, result.Errors.Count);
    }
}

public class MaxRecordCountSpecificationTests
{
    [Fact]
    public void CountWithinLimit_IsSatisfied()
    {
        Assert.True(new MaxRecordCountSpecification(3).IsSatisfiedBy(JsonNode.Parse("[1,2,3]")!).IsSuccess);
    }

    [Fact]
    public void CountAboveLimit_FailsWithTooManyRecords()
    {
        var result = new MaxRecordCountSpecification(2).IsSatisfiedBy(JsonNode.Parse("[1,2,3]")!);

        var error = Assert.IsType<TooLargeError.TooManyRecords>(Assert.Single(result.Errors));
        Assert.Equal(3, error.Count);
        Assert.Equal(2, error.MaxRecords);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveLimit_IsAProgrammerError(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaxRecordCountSpecification(limit));
    }
}
