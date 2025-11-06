using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using RestSQL.Application.Interfaces;

namespace RestSQL.Application;

public class RequestBodyParser : IRequestBodyParser
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILogger<RequestBodyParser> _logger;

    public RequestBodyParser(ILogger<RequestBodyParser> logger)
    {
        _logger = logger;
    }

    public RequestBodyParser() : this(NullLogger<RequestBodyParser>.Instance)
    {
    }

    public async Task<JsonNode?> ReadAndParseJsonStreamAsync(Stream? stream)
    {
        if (stream is null)
        {
            _logger.LogDebug("No request body stream provided to parser.");
            return null;
        }

        JsonNode? jsonValue = null;

        try
        {
            jsonValue = await JsonSerializer.DeserializeAsync<JsonNode>(stream, options).ConfigureAwait(false);
            _logger.LogDebug("Parsed request body into JsonNode: {hasValue}", jsonValue is not null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse request body as JSON. Returning null.");
            // Ignore parsing errors, return null for jsonValue
        }

        return jsonValue;
    }
}