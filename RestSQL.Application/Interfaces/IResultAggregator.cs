using System.Text.Json.Nodes;
using RestSQL.Domain;

namespace RestSQL.Application.Interfaces;

public interface IResultAggregator
{
    JsonNode? Aggregate(IDictionary<string, IEnumerable<IDictionary<string, object?>>> queryResults, OutputField jsonStructure);
}
