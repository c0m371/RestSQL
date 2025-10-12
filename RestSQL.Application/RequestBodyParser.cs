using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestSQL.Application.Interfaces;

namespace RestSQL.Application;

public class RequestBodyParser : IRequestBodyParser
{
    public async Task<JsonNode?> ReadAndParseJsonStreamAsync(Stream? stream)
    {
        if (stream is null)
            return null;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        return await JsonSerializer.DeserializeAsync<JsonNode>(stream, options);
    }
}
