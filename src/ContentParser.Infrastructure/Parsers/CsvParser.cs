using System.Text;

using ContentParser.Parser.Models;
using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Results;
using ContentParser.Parser.Results.Errors;

using Microsoft.Extensions.Options;

namespace ContentParser.Parser.Parsers;

/// <summary>
/// Parser CSV zgodny z RFC 4180: pierwszy wiersz to naglowek, pola moga byc cytowane,
/// a wewnatrz cudzyslowow dozwolone sa przecinki, znaki nowej linii i podwojone cudzyslowy ("").
/// Napisany recznie, zeby obsluga cytowania i bledow byla widoczna, a nie schowana w bibliotece.
/// </summary>
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

        return Tokenize(content).Bind(BuildRecords);
    }

    private Result<ParseResult> BuildRecords(List<List<string>> rows)
    {
        if (rows.Count == 0)
        {
            return Result<ParseResult>.Fail(new ValidationError.CsvHeaderIsMissing());
        }

        var headerResult = ReadHeader(rows[0]);
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

            // Numer wiersza liczony od 1 razem z naglowkiem — tak, jak widzi go uzytkownik w pliku.
            if (row.Count != header.Length)
            {
                errors.Add(new ValidationError.CsvRowHasWrongFieldCount(i + 1, header.Length, row.Count));
                continue;
            }

            var fields = new Dictionary<string, string?>(header.Length, StringComparer.Ordinal);
            for (var column = 0; column < header.Length; column++)
            {
                fields[header[column]] = row[column];
            }

            records.Add(new ParsedRecord(fields));
        }

        return errors.Count > 0
            ? Result<ParseResult>.FromErrors(errors)
            : Result<ParseResult>.Ok(new ParseResult(records.Count, records));
    }

    private static Result<string[]> ReadHeader(List<string> headerRow)
    {
        List<Error> errors = [];
        var names = new string[headerRow.Count];
        HashSet<string> seen = new(StringComparer.Ordinal);

        for (var i = 0; i < headerRow.Count; i++)
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

    /// <summary>Maszyna stanow RFC 4180. Obsluguje LF i CRLF oraz cudzyslowy wielolinijkowe.</summary>
    private static Result<List<List<string>>> Tokenize(string content)
    {
        List<List<string>> rows = [];
        List<string> row = [];
        var field = new StringBuilder();

        var inQuotes = false;
        var quoteStartedAtRow = 1;
        var currentRow = 1;
        var index = 0;

        while (index < content.Length)
        {
            var character = content[index];

            if (inQuotes)
            {
                if (character == Quote)
                {
                    // "" wewnatrz cytowanego pola to zaescapowany cudzyslow.
                    if (index + 1 < content.Length && content[index + 1] == Quote)
                    {
                        field.Append(Quote);
                        index += 2;
                        continue;
                    }

                    inQuotes = false;
                    index++;
                    continue;
                }

                if (character == '\n')
                {
                    currentRow++;
                }

                field.Append(character);
                index++;
                continue;
            }

            switch (character)
            {
                case Quote when field.Length == 0:
                    inQuotes = true;
                    quoteStartedAtRow = currentRow;
                    index++;
                    break;

                case Delimiter:
                    row.Add(field.ToString());
                    field.Clear();
                    index++;
                    break;

                case '\r':
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    AppendRow(rows, row);
                    row = [];
                    currentRow++;
                    index += character == '\r' && index + 1 < content.Length && content[index + 1] == '\n' ? 2 : 1;
                    break;

                default:
                    field.Append(character);
                    index++;
                    break;
            }
        }

        if (inQuotes)
        {
            return Result<List<List<string>>>.Fail(new ValidationError.CsvQuotedFieldNotClosed(quoteStartedAtRow));
        }

        if (row.Count > 0 || field.Length > 0)
        {
            row.Add(field.ToString());
            AppendRow(rows, row);
        }

        return Result<List<List<string>>>.Ok(rows);
    }

    /// <summary>Pomija wiersze calkowicie puste — konczace znaki nowej linii i puste linie w srodku.</summary>
    private static void AppendRow(List<List<string>> rows, List<string> row)
    {
        if (row is [""])
        {
            return;
        }

        rows.Add(row);
    }
}
