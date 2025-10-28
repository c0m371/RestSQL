using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestSQL.Application.Interfaces;
using System.IO;
using System.Threading.Tasks;

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

        JsonNode? jsonValue = null;

        try
        {
            jsonValue = await JsonSerializer.DeserializeAsync<JsonNode>(stream, options).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Ignore parsing errors, return null for jsonValue
        }

        return jsonValue;
    }
}