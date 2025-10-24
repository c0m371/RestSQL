using System;
using System.Text.Json.Nodes;

namespace RestSQL.Application.Interfaces;

public interface IRequestBodyParser
{
    Task<(JsonNode? jsonValue, string? stringValue)> ReadAndParseJsonStreamAsync(Stream? stream);
}
