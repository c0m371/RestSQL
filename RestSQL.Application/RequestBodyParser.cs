using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestSQL.Application.Interfaces;

namespace RestSQL.Application;

public class RequestBodyParser : IRequestBodyParser
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<JsonNode?> ReadAndParseJsonStreamAsync(Stream? stream)
    {
        if (stream is null)
            return null;

        return await JsonSerializer.DeserializeAsync<JsonNode>(stream, options);
    }
}
