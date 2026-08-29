using System.Text.Json.Nodes;

using ContentParser.Core.Results.Errors;
using ContentParser.Core.Tests.TestDoubles;
using ContentParser.Core.Validation.Specifications;

namespace ContentParser.Core.Tests.Validation;

internal static class Specs
{
    public static readonly Error Reason = new TestError("negacja", "Specyfikacja jest spelniona, a nie powinna.");

    public static Specification<string> Of(bool satisfied) =>
        satisfied ? new AlwaysTrueSpecification<string>() : new AlwaysFalseSpecification<string>("nie");

    public static bool Holds(this Specification<string> specification) =>
        specification.IsSatisfiedBy("x").IsSuccess;
}

public class AndSpecificationTests
{
    [Fact]
    public void BothSatisfied_IsSuccess()
    {
        var spec = new AlwaysTrueSpecification<string>().And(new AlwaysTrueSpecification<string>());

        Assert.True(spec.IsSatisfiedBy("x").IsSuccess);
    }

    [Fact]
    public void LeftFails_ReturnsLeftError()
    {
        var spec = new AlwaysFalseSpecification<string>("left").And(new AlwaysTrueSpecification<string>());

        Assert.Equal("left", Assert.Single(spec.IsSatisfiedBy("x").Errors).Code);
    }

    [Fact]
    public void RightFails_ReturnsRightError()
    {
        var spec = new AlwaysTrueSpecification<string>().And(new AlwaysFalseSpecification<string>("right"));

        Assert.Equal("right", Assert.Single(spec.IsSatisfiedBy("x").Errors).Code);
    }

    [Fact]
    public void BothFail_AggregatesErrorsInOrder()
    {
        var spec = new AlwaysFalseSpecification<string>("left").And(new AlwaysFalseSpecification<string>("right"));

        Assert.Collection(
            spec.IsSatisfiedBy("x").Errors,
            e => Assert.Equal("left", e.Code),
            e => Assert.Equal("right", e.Code));
    }

    [Fact]
    public void ChainedAnd_EvaluatesEverySpecification()
    {
        var spec = new AlwaysFalseSpecification<string>("a")
            .And(new AlwaysFalseSpecification<string>("b"))
            .And(new AlwaysFalseSpecification<string>("c"));

        Assert.Equal(3, spec.IsSatisfiedBy("x").Errors.Count);
    }

    [Fact]
    public void And_DoesNotShortCircuit_SoThatEveryErrorIsReported()
    {
        var right = new CountingSpecification<string>(new AlwaysFalseSpecification<string>("right"));

        new AlwaysFalseSpecification<string>("left").And(right).IsSatisfiedBy("x");

        Assert.Equal(1, right.Evaluations);
    }

    [Fact]
    public void And_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlwaysTrueSpecification<string>().And(null!));
    }
}

public class OrSpecificationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AtLeastOneSatisfied_IsSuccess(bool left, bool right)
    {
        Assert.True(Specs.Of(left).Or(Specs.Of(right)).Holds());
    }

    [Fact]
    public void NeitherSatisfied_Fails()
    {
        Assert.False(Specs.Of(false).Or(Specs.Of(false)).Holds());
    }

    [Fact]
    public void BothFail_AggregatesErrorsInOrder()
    {
        var spec = new AlwaysFalseSpecification<string>("left").Or(new AlwaysFalseSpecification<string>("right"));

        Assert.Collection(
            spec.IsSatisfiedBy("x").Errors,
            e => Assert.Equal("left", e.Code),
            e => Assert.Equal("right", e.Code));
    }

    [Fact]
    public void LeftSatisfied_DiscardsRightErrors()
    {
        var spec = new AlwaysTrueSpecification<string>().Or(new AlwaysFalseSpecification<string>("right"));

        Assert.Empty(spec.IsSatisfiedBy("x").Errors);
    }

    [Fact]
    public void LeftSatisfied_ShortCircuits_BecauseRightErrorsCouldNotBeReported()
    {
        var right = new CountingSpecification<string>(new AlwaysFalseSpecification<string>("right"));

        new AlwaysTrueSpecification<string>().Or(right).IsSatisfiedBy("x");

        Assert.Equal(0, right.Evaluations);
    }

    [Fact]
    public void Or_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlwaysTrueSpecification<string>().Or(null!));
    }
}

public class NotSpecificationTests
{
    [Fact]
    public void InnerSatisfied_FailsWithTheSuppliedError()
    {
        var spec = new AlwaysTrueSpecification<string>().Not(Specs.Reason);

        Assert.Same(Specs.Reason, Assert.Single(spec.IsSatisfiedBy("x").Errors));
    }

    [Fact]
    public void InnerNotSatisfied_IsSuccess_AndDropsInnerErrors()
    {
        var spec = new AlwaysFalseSpecification<string>("wewnetrzny").Not(Specs.Reason);

        var result = spec.IsSatisfiedBy("x");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Not_WithNullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlwaysTrueSpecification<string>().Not(null!));
    }
}

public class SpecificationLawsTests
{
    public static TheoryData<bool, bool> Pairs => new() { { true, true }, { true, false }, { false, true }, { false, false } };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void DeMorgan_NegatedAnd_EqualsOrOfNegations(bool left, bool right)
    {
        var a = Specs.Of(left);
        var b = Specs.Of(right);

        Assert.Equal(
            a.And(b).Not(Specs.Reason).Holds(),
            a.Not(Specs.Reason).Or(b.Not(Specs.Reason)).Holds());
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void DeMorgan_NegatedOr_EqualsAndOfNegations(bool left, bool right)
    {
        var a = Specs.Of(left);
        var b = Specs.Of(right);

        Assert.Equal(
            a.Or(b).Not(Specs.Reason).Holds(),
            a.Not(Specs.Reason).And(b.Not(Specs.Reason)).Holds());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoubleNegation_EqualsTheOriginal(bool satisfied)
    {
        var a = Specs.Of(satisfied);

        Assert.Equal(a.Holds(), a.Not(Specs.Reason).Not(Specs.Reason).Holds());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExcludedMiddle_DisjunctionWithOwnNegation_AlwaysHolds(bool satisfied)
    {
        var a = Specs.Of(satisfied);

        Assert.True(a.Or(a.Not(Specs.Reason)).Holds());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NonContradiction_ConjunctionWithOwnNegation_NeverHolds(bool satisfied)
    {
        var a = Specs.Of(satisfied);

        Assert.False(a.And(a.Not(Specs.Reason)).Holds());
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void And_IsCommutative_InSatisfaction(bool left, bool right)
    {
        Assert.Equal(
            Specs.Of(left).And(Specs.Of(right)).Holds(),
            Specs.Of(right).And(Specs.Of(left)).Holds());
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void Or_IsCommutative_InSatisfaction(bool left, bool right)
    {
        Assert.Equal(
            Specs.Of(left).Or(Specs.Of(right)).Holds(),
            Specs.Of(right).Or(Specs.Of(left)).Holds());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void And_IsAssociative_InSatisfaction(bool middle)
    {
        var a = Specs.Of(true);
        var b = Specs.Of(middle);
        var c = Specs.Of(false);

        Assert.Equal(a.And(b).And(c).Holds(), a.And(b.And(c)).Holds());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Or_IsAssociative_InSatisfaction(bool middle)
    {
        var a = Specs.Of(false);
        var b = Specs.Of(middle);
        var c = Specs.Of(false);

        Assert.Equal(a.Or(b).Or(c).Holds(), a.Or(b.Or(c)).Holds());
    }
}

public class WhenArraySpecificationTests
{
    private static JsonNode Node(string json) => JsonNode.Parse(json)!;

    [Theory]
    [InlineData("""{"id":1}""")]
    [InlineData("42")]
    [InlineData("\"text\"")]
    public void NonArrayNode_IsVacuouslySatisfied(string json)
    {
        var spec = new WhenArraySpecification(new MaxRecordCountSpecification(1));

        Assert.True(spec.IsSatisfiedBy(Node(json)).IsSuccess);
    }

    [Fact]
    public void NonArrayNode_DoesNotEvaluateTheRule()
    {
        var rule = new CountingSpecification<JsonArray>(new MaxRecordCountSpecification(1));

        new WhenArraySpecification(rule).IsSatisfiedBy(Node("""{"id":1}"""));

        Assert.Equal(0, rule.Evaluations);
    }

    [Fact]
    public void Array_DelegatesToTheRule()
    {
        var spec = new WhenArraySpecification(new MaxRecordCountSpecification(1));

        Assert.IsType<TooLargeError.TooManyRecords>(Assert.Single(spec.IsSatisfiedBy(Node("[1,2]")).Errors));
    }

    [Fact]
    public void WithNullRule_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WhenArraySpecification(null!));
    }
}
