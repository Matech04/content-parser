namespace ContentParser.Parser.Results.Errors;

/// <summary>
/// Zadanie jest poprawne skladniowo, ale przeslana tresc nie spelnia kontraktu.
/// Mapowane na 422 Unprocessable Entity.
/// </summary>
public abstract record ValidationError(string Code, string Message) : Error(Code, Message)
{
    public sealed record ContentIsEmpty()
        : ValidationError("content-empty", "Content is empty.");

    public sealed record IncorrectContentBase64Encoding()
        : ValidationError("incorrect-base64", "Content is not valid Base64.");

    public sealed record ContentIsNotValidUtf8()
        : ValidationError("invalid-utf8", "Decoded content is not valid UTF-8 text.");

    public sealed record IncorrectJson(string Details)
        : ValidationError("incorrect-json", Details);

    public sealed record JsonIsNotAnArray()
        : ValidationError("json-not-array", "Root element must be a JSON array.");

    public sealed record JsonElementIsNotAnObject(int Index)
        : ValidationError("json-element-not-object", $"Element at index {Index} is not a JSON object.");

    public sealed record JsonValueIsNested(int Index, string PropertyName)
        : ValidationError(
            "json-value-nested",
            $"Property '{PropertyName}' at index {Index} holds a nested object or array; only flat records are supported.");

    public sealed record JsonPropertyNameIsEmpty(int Index)
        : ValidationError("json-property-name-empty", $"Element at index {Index} has an empty property name.");

    public sealed record JsonKeysAreNotUniform(int Index, IReadOnlyList<string> Expected, IReadOnlyList<string> Actual)
        : ValidationError(
            "json-keys-not-uniform",
            $"Element at index {Index} has properties [{string.Join(", ", Actual)}] "
            + $"but [{string.Join(", ", Expected)}] was expected; all records must share the same fields.");

    public sealed record CsvHeaderIsMissing()
        : ValidationError("csv-header-missing", "CSV must start with a header row.");

    public sealed record CsvColumnNameIsEmpty(int ColumnIndex)
        : ValidationError("csv-column-name-empty", $"Header column at index {ColumnIndex} has an empty name.");

    public sealed record CsvDuplicateColumn(string ColumnName)
        : ValidationError("csv-duplicate-column", $"Header contains duplicate column '{ColumnName}'.");

    public sealed record CsvRowHasWrongFieldCount(int RowNumber, int Expected, int Actual)
        : ValidationError(
            "csv-row-field-count",
            $"Row {RowNumber} has {Actual} field(s) but the header declares {Expected}.");

    public sealed record CsvQuotedFieldNotClosed(int RowNumber)
        : ValidationError("csv-unterminated-quote", $"Row {RowNumber} contains a quoted field that is never closed.");
}
