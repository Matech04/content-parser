using ContentParser.Infrastructure.Tests.TestDoubles;

namespace ContentParser.Infrastructure.Tests.Validation;

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
        // Celowy brak short-circuit: uzytkownik ma dostac komplet bledow naraz.
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
    public void And_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlwaysTrueSpecification<string>().And(null!));
    }
}
