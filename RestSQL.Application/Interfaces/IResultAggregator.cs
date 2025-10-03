using System.Text.Json.Nodes;
using RestSQL.Config;

namespace RestSQL.Application.Interfaces;

public interface IResultAggregator
{
    JsonNode? Aggregate(IDictionary<string, IEnumerable<IDictionary<string, object?>>> queryResults, OutputField jsonStructure);
}
