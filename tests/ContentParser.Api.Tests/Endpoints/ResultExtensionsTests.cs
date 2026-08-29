using ContentParser.Api.Endpoints;
using ContentParser.Api.Tests.Infrastructure;

using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

using Microsoft.AspNetCore.Http;

namespace ContentParser.Api.Tests.Endpoints;

public class ResultExtensionsTests
{
    private sealed record UnknownError() : Error("boom", "Something went wrong");

    [Fact]
    public async Task Success_NonGeneric_MapsTo200()
    {
        var response = await HttpResultExecutor.ExecuteAsync(Result.Ok().ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
    }

    [Fact]
    public async Task Success_Generic_MapsTo200_AndSerializesValue()
    {
        var response = await HttpResultExecutor.ExecuteAsync(Result<int>.Ok(42).ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(42, response.Json.GetInt32());
    }

    [Fact]
    public async Task TooLargeError_MapsTo413()
    {
        var response = await HttpResultExecutor.ExecuteAsync(
            Result.Fail(new TooLargeError.ContentIsTooLarge(1024)).ToHttpResult());

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, response.StatusCode);
        Assert.Equal("Content Too Large", response.GetString("title"));
        Assert.EndsWith("content-too-large", response.GetString("type"));
    }

    [Theory]
    [MemberData(nameof(ValidationErrors))]
    public async Task ValidationError_MapsTo422(ValidationError error)
    {
        var response = await HttpResultExecutor.ExecuteAsync(Result.Fail(error).ToHttpResult());

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal("Validation Error", response.GetString("title"));
        Assert.Equal(error.Message, response.GetString("detail"));
        Assert.EndsWith(error.Code, response.GetString("type"));
    }

    public static TheoryData<ValidationError> ValidationErrors() =>
    [
        new ValidationError.ContentIsEmpty(),
        new ValidationError.IncorrectContentBase64Encoding(),
        new ValidationError.ContentIsNotValidUtf8(),
        new ValidationError.IncorrectJson("unexpected token"),
        new ValidationError.JsonIsNotAnArray(),
        new ValidationError.CsvHeaderIsMissing(),
        new ValidationError.CsvRowHasWrongFieldCount(2, 3, 2),
    ];

    [Theory]
    [MemberData(nameof(RequestErrors))]
    public async Task RequestError_MapsTo400(RequestError error)
    {
        var response = await HttpResultExecutor.ExecuteAsync(Result.Fail(error).ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal("Bad Request", response.GetString("title"));
    }

    public static TheoryData<RequestError> RequestErrors() =>
    [
        new RequestError.TypeIsMissing(),
        new RequestError.UnsupportedParser("XML", ["CSV", "INTERNAL_JSON"]),
    ];

    [Fact]
    public async Task Errors_UseProblemJsonContentType()
    {
        var response = await HttpResultExecutor.ExecuteAsync(
            Result.Fail(new ValidationError.ContentIsEmpty()).ToHttpResult());

        Assert.StartsWith("application/problem+json", response.ContentType);
    }

    [Fact]
    public async Task UnknownErrorType_FallsBackTo400()
    {
        var response = await HttpResultExecutor.ExecuteAsync(Result.Fail(new UnknownError()).ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal("Something went wrong", response.GetString("detail"));
    }

    [Fact]
    public async Task MultipleErrors_FirstErrorDeterminesStatus()
    {
        var response = await HttpResultExecutor.ExecuteAsync(
            Result.Fail(new TooLargeError.ContentIsTooLarge(1), new ValidationError.ContentIsEmpty()).ToHttpResult());

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task MultipleErrors_AreAllExposedUnderErrorsExtension()
    {
        var response = await HttpResultExecutor.ExecuteAsync(
            Result.Fail(
                new ValidationError.JsonElementIsNotAnObject(1),
                new ValidationError.JsonKeysAreNotUniform(2, ["a"], ["b"])).ToHttpResult());

        var errors = response.Json.GetProperty("errors");

        Assert.Equal(2, errors.GetArrayLength());
        Assert.Equal("json-element-not-object", errors[0].GetProperty("code").GetString());
        Assert.Equal("json-keys-not-uniform", errors[1].GetProperty("code").GetString());
    }

    [Fact]
    public async Task FailedGenericResult_NeverReachesForTheValue()
    {
        var response = await HttpResultExecutor.ExecuteAsync(
            Result<string>.Fail(new ValidationError.ContentIsEmpty()).ToHttpResult());

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
    }
}
