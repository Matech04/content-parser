using ContentParser.Core.Tests.TestDoubles;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

namespace ContentParser.Core.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Ok_NonGeneric_IsSuccess_And_HasNoErrors()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fail_NonGeneric_IsNotSuccess_And_KeepsError()
    {
        var error = new ValidationError.ContentIsEmpty();

        var result = Result.Fail(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Fail_WithNullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Fail(null!));
    }

    [Fact]
    public void Fail_KeepsAllErrorsInOrder()
    {
        var first = new TestError("a", "a");
        var second = new TestError("b", "b");

        var result = Result<int>.Fail(first, second);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors, e => Assert.Same(first, e), e => Assert.Same(second, e));
    }

    [Fact]
    public void TryGetValue_OnSuccess_ReturnsTrueAndValue()
    {
        var result = Result<string>.Ok("payload");

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal("payload", value);
    }

    [Fact]
    public void TryGetValue_OnFailure_ReturnsFalse_AndDoesNotThrow()
    {
        var result = Result<string>.Fail(new ValidationError.ContentIsEmpty());

        Assert.False(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Ok_AllowsNullValue()
    {
        var result = Result<string?>.Ok(null);

        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Match_OnSuccess_RunsSuccessBranch()
    {
        var outcome = Result<int>.Ok(21).Match(value => value * 2, _ => -1);

        Assert.Equal(42, outcome);
    }

    [Fact]
    public void Match_OnFailure_RunsFailureBranch_WithAllErrors()
    {
        var outcome = Result<int>
            .Fail(new TestError("a", "a"), new TestError("b", "b"))
            .Match(_ => "ok", errors => string.Join(",", errors.Select(e => e.Code)));

        Assert.Equal("a,b", outcome);
    }

    [Fact]
    public void Bind_OnSuccess_RunsNextStep()
    {
        var result = Result<int>.Ok(2).Bind(value => Result<string>.Ok($"v{value}"));

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal("v2", value);
    }

    [Fact]
    public void Bind_OnFailure_SkipsNextStep_AndCarriesErrors()
    {
        var called = false;

        var result = Result<int>
            .Fail(new ValidationError.ContentIsEmpty())
            .Bind(_ => { called = true; return Result<string>.Ok("never"); });

        Assert.False(called);
        Assert.IsType<ValidationError.ContentIsEmpty>(Assert.Single(result.Errors));
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var result = Result<int>.Ok(3).Map(value => value.ToString());

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal("3", value);
    }

    [Fact]
    public void Map_OnFailure_SkipsMapper_AndCarriesErrors()
    {
        var called = false;

        var result = Result<int>
            .Fail(new TestError("x", "x"))
            .Map(_ => { called = true; return "never"; });

        Assert.False(called);
        Assert.Equal("x", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void NonGenericBind_CarriesErrorsIntoGenericResult()
    {
        var result = Result.Fail(new TestError("x", "x")).Bind(() => Result<int>.Ok(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("x", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void ToString_OnFailure_DoesNotThrow_AndListsErrorCodes()
    {
        var result = Result<string>.Fail(new TestError("a", "a"), new TestError("b", "b"));

        Assert.Equal("Failure<String>(a, b)", result.ToString());
    }

    [Fact]
    public void ToString_OnSuccess_DoesNotLeakValue()
    {
        Assert.Equal("Success<String>", Result<string>.Ok("sekret").ToString());
    }
}
