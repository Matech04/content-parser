using System.Text.Json;

using ContentParser.Api.Contracts.V1;

namespace ContentParser.Api.Tests.Contracts;

public class ParseContentRequestDtoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DeserializesFromCamelCaseJson()
    {
        var dto = JsonSerializer.Deserialize<ParseContentRequestDto>(
            """{"type":"INTERNAL_JSON","content":"W10="}""", Web);

        Assert.NotNull(dto);
        Assert.Equal("INTERNAL_JSON", dto.Type);
        Assert.Equal("W10=", dto.Content);
    }

    [Fact]
    public void MissingProperties_DeserializeToNull()
    {
        // NRT nie sa egzekwowane w runtime — dlatego wlasciwosci sa jawnie nullowalne.
        var dto = JsonSerializer.Deserialize<ParseContentRequestDto>("{}", Web);

        Assert.NotNull(dto);
        Assert.Null(dto.Type);
        Assert.Null(dto.Content);
    }
}
