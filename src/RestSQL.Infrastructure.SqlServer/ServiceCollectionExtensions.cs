using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.SqlServer;

public static class ServiceCollectionExtensions
{
    public static void AddSqlServer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, SqlServerQueryExecutor>();
        serviceCollection.AddSingleton<SqlServerConnectionFactory>();
    }
}
