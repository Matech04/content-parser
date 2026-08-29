namespace ContentParser.Core.Results.Errors;

public abstract record RequestError(string Code, string Message) : Error(Code, Message)
{
    public sealed record TypeIsMissing()
        : RequestError("type-missing", "Field 'type' is required.");

    public sealed record UnsupportedParser(string RequestedType, IReadOnlyList<string> SupportedTypes)
        : RequestError(
            "unsupported-parser",
            $"Content type '{RequestedType}' is not supported. Supported types: {string.Join(", ", SupportedTypes)}.");
}
