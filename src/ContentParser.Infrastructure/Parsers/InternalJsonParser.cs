using System.Text.Json;
using System.Text.Json.Nodes;

using ContentParser.Parser.Models;
using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;
using ContentParser.Parser.Validation.Builders;
using ContentParser.Parser.Validation.Specifications;

using Microsoft.Extensions.Options;

namespace ContentParser.Parser.Parsers;

/// <summary>
/// INTERNAL_JSON = tablica plaskich, jednorodnych obiektow. Ten ksztalt jest celowy:
/// dokladnie taki sam zbior danych opisuje CSV, wiec obie sciezki daja te sama strukture wyjsciowa.
/// </summary>
public sealed class InternalJsonParser : IContentParser
{
    private readonly Specification<JsonNode> _validator;

    public InternalJsonParser(IOptions<ParsingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Budowane raz, nie przy kazdym zadaniu — specyfikacje sa bezstanowe.
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

        return _validator.IsSatisfiedBy(root).Bind(() => Result<ParseResult>.Ok(ToParseResult((JsonArray)root)));
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

    // Ujednolicona struktura trzyma wartosci jako tekst — CSV nie zna typow,
    // wiec sprowadzamy JSON do wspolnego mianownika. null zostaje nullem.
    private static string? ToText(JsonNode? value) => value switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var text) => text,
        _ => value.ToJsonString(),
    };
}
