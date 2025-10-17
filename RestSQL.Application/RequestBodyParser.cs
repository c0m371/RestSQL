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

    public (JsonNode? jsonValue, string? stringValue) ReadAndParseJsonStream(Stream? stream)
    {
        if (stream is null)
            return (null, null);

        var reader = new StreamReader(stream);
        var stringValue = reader.ReadToEnd();

        JsonNode? jsonValue = null;

        try
        {
            jsonValue = JsonSerializer.Deserialize<JsonNode>(stringValue, options);
        }
        catch (JsonException)
        {
            // Ignore parsing errors, return null for jsonValue
        }

        return (jsonValue, stringValue);
    }
}
