using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.MySql;

public static class ServiceCollectionExtensions
{
  public static void AddMySql(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, MySqlQueryExecutor>();
        serviceCollection.AddSingleton<MySqlConnectionFactory>();
    }
}
