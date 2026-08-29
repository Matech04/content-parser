using System.Text.Json;
using System.Text.Json.Nodes;

using ContentParser.Core.Models;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;
using ContentParser.Core.Validation.Builders;
using ContentParser.Core.Validation.Specifications;

using Microsoft.Extensions.Options;

namespace ContentParser.Core.Parsers;

public sealed class InternalJsonParser : IContentParser
{
    private readonly Specification<JsonNode> _validator;

    public InternalJsonParser(IOptions<ParsingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _validator = new JsonValidatorBuilder()
            .EnsureIsArray()
            .EnsureAllElementsAreObjects()
            .EnsurePropertyNamesAreNotEmpty()
            .EnsureAllValuesAreFlat()
            .EnsureKeysAreUniform()
            .EnsureAtMostRecords(options.Value.MaxRecords)
            .Build();
    }

    public string Type => "INTERNAL_JSON";

    public Result<ParseResult> TryParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<ParseResult>.Fail(new ValidationError.ContentIsEmpty());
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content);
        }
        catch (JsonException ex)
        {
            return Result<ParseResult>.Fail(new ValidationError.IncorrectJson(ex.Message));
        }

        if (root is null)
        {
            return Result<ParseResult>.Fail(new ValidationError.ContentIsEmpty());
        }

        return _validator.IsSatisfiedBy(root).Bind(() => root is JsonArray array
            ? Result<ParseResult>.Ok(ToParseResult(array))
            : Result<ParseResult>.Fail(new ValidationError.JsonIsNotAnArray()));
    }

    private static ParseResult ToParseResult(JsonArray array)
    {
        List<ParsedRecord> records = new(array.Count);

        foreach (var element in array)
        {
            var fields = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var (name, value) in (JsonObject)element!)
            {
                fields[name] = ToText(value);
            }

            records.Add(new ParsedRecord(fields));
        }

        return new ParseResult(records.Count, records);
    }

    private static string? ToText(JsonNode? value) => value switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var text) => text,
        _ => value.ToJsonString(),
    };
}
