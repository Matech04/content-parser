namespace ContentParser.Core.Results.Errors;

public abstract record TooLargeError(string Code, string Message) : Error(Code, Message)
{
    public sealed record ContentIsTooLarge(int MaxBytes)
        : TooLargeError("content-too-large", $"Decoded content exceeds the limit of {MaxBytes} bytes.");

    public sealed record TooManyRecords(int Count, int MaxRecords)
        : TooLargeError("too-many-records", $"Content holds {Count} records; the limit is {MaxRecords}.");
}
