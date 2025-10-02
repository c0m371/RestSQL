using RestSQL.Config;

namespace RestSQL.Application.Interfaces;

public interface IResultAggregator
{
    object? Aggregate(IDictionary<string, IEnumerable<dynamic>> queryResults, OutputField jsonStructure);
}
