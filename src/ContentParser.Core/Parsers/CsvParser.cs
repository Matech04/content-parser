using System.Globalization;

using ContentParser.Core.Models;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Results;
using ContentParser.Core.Results.Errors;

using CsvHelper.Configuration;

using Microsoft.Extensions.Options;

namespace ContentParser.Core.Parsers;

public sealed class CsvParser : IContentParser
{
    private const char Delimiter = ',';
    private const char Quote = '"';

    private readonly int _maxRecords;

    public CsvParser(IOptions<ParsingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _maxRecords = options.Value.MaxRecords;
    }

    public string Type => "CSV";

    public Result<ParseResult> TryParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<ParseResult>.Fail(new ValidationError.ContentIsEmpty());
        }

        return ReadRows(content).Bind(BuildRecords);
    }

    private sealed record CsvRow(int Number, string[] Fields);

    private static Result<List<CsvRow>> ReadRows(string content)
    {
        string? badField = null;

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = Delimiter.ToString(),
            Quote = Quote,
            HasHeaderRecord = false,
            IgnoreBlankLines = true,
            DetectColumnCountChanges = false,
            BadDataFound = args => badField = args.Field,
        };

        using var reader = new StringReader(content);
        using var parser = new CsvHelper.CsvParser(reader, configuration);

        List<CsvRow> rows = [];
        while (parser.Read())
        {
            rows.Add(new CsvRow(parser.Row, parser.Record ?? []));
        }

        return badField is not null && rows.Count > 0 && IsQuoteNeverClosed(badField)
            ? Result<List<CsvRow>>.Fail(new ValidationError.CsvQuotedFieldNotClosed(rows[^1].Number))
            : Result<List<CsvRow>>.Ok(rows);
    }

    private static bool IsQuoteNeverClosed(string rawField) =>
        rawField.Length > 0 && rawField[0] == Quote && rawField.Count(character => character == Quote) % 2 != 0;

    private Result<ParseResult> BuildRecords(List<CsvRow> rows)
    {
        if (rows.Count == 0)
        {
            return Result<ParseResult>.Fail(new ValidationError.CsvHeaderIsMissing());
        }

        var headerResult = ReadHeader(rows[0].Fields);
        if (!headerResult.TryGetValue(out var header))
        {
            return Result<ParseResult>.FromErrors(headerResult.Errors);
        }

        var dataRowCount = rows.Count - 1;
        if (dataRowCount > _maxRecords)
        {
            return Result<ParseResult>.Fail(new TooLargeError.TooManyRecords(dataRowCount, _maxRecords));
        }

        List<Error> errors = [];
        List<ParsedRecord> records = new(dataRowCount);

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];

            if (row.Fields.Length != header.Length)
            {
                errors.Add(new ValidationError.CsvRowHasWrongFieldCount(row.Number, header.Length, row.Fields.Length));
                continue;
            }

            var fields = new Dictionary<string, string?>(header.Length, StringComparer.Ordinal);
            for (var column = 0; column < header.Length; column++)
            {
                fields[header[column]] = row.Fields[column];
            }

            records.Add(new ParsedRecord(fields));
        }

        return errors.Count > 0
            ? Result<ParseResult>.FromErrors(errors)
            : Result<ParseResult>.Ok(new ParseResult(records.Count, records));
    }

    private static Result<string[]> ReadHeader(string[] headerRow)
    {
        List<Error> errors = [];
        var names = new string[headerRow.Length];
        HashSet<string> seen = new(StringComparer.Ordinal);

        for (var i = 0; i < headerRow.Length; i++)
        {
            var name = headerRow[i].Trim();

            if (name.Length == 0)
            {
                errors.Add(new ValidationError.CsvColumnNameIsEmpty(i));
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add(new ValidationError.CsvDuplicateColumn(name));
                continue;
            }

            names[i] = name;
        }

        return errors.Count > 0 ? Result<string[]>.FromErrors(errors) : Result<string[]>.Ok(names);
    }
}
