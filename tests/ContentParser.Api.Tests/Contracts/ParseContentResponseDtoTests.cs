using System.Text.Json;

using ContentParser.Api.Contracts.V1;

using ContentParser.Core.Models;

namespace ContentParser.Api.Tests.Contracts;

public class ParseContentResponseDtoTests
{
    [Fact]
    public void From_ProjectsRecordsIntoFlatObjects()
    {
        var parseResult = new ParseResult(1, [new ParsedRecord(new Dictionary<string, string?> { ["id"] = "1" })]);

        var dto = ParseContentResponseDto.From(parseResult);

        Assert.Equal("Success", dto.Status);
        Assert.Equal(1, dto.ProcessedCount);
        Assert.Equal("1", Assert.Single(dto.Data)["id"]);
    }

    [Fact]
    public void SerializesRecordsAsJsonObjects_NotAsNestedDictionaries()
    {
        var parseResult = new ParseResult(
            1,
            [new ParsedRecord(new Dictionary<string, string?> { ["id"] = "1", ["name"] = null })]);

        var json = JsonSerializer.Serialize(
            ParseContentResponseDto.From(parseResult),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("""{"status":"Success","processedCount":1,"data":[{"id":"1","name":null}]}""", json);
    }
}
