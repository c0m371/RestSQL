using System.Text.Json.Nodes;
using RestSQL.Config;

namespace RestSQL.Application.Interfaces;

public interface IResultAggregator
{
    JsonNode? Aggregate(IDictionary<string, IEnumerable<dynamic>> queryResults, OutputField jsonStructure);
}
