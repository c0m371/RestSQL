using System;
using System.Text.Json.Nodes;

namespace RestSQL.Application.Interfaces;

public interface IRequestBodyParser
{
    (JsonNode? jsonValue, string? stringValue) ReadAndParseJsonStream(Stream? stream);
}
