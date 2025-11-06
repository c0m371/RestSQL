using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Oracle;

public static class ServiceCollectionExtensions
{
  public static void AddOracle(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, OracleQueryExecutor>();
        serviceCollection.AddSingleton<OracleConnectionFactory>();
    }
}
