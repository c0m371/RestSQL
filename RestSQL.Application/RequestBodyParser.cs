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

    public async Task<(JsonNode? jsonValue, string? stringValue)> ReadAndParseJsonStreamAsync(Stream? stream)
    {
        if (stream is null)
            return (null, null);

        if (!stream.CanSeek)
        {
            // Copy the content to a seekable MemoryStream
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            stream = memoryStream;
            stream.Position = 0;
        }

        string? stringValue = null;

        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            stringValue = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        stream.Position = 0;

        JsonNode? jsonValue = null;

        try
        {
            jsonValue = await JsonSerializer.DeserializeAsync<JsonNode>(stream, options).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Ignore parsing errors, return null for jsonValue
        }

        return (jsonValue, stringValue);
    }
}