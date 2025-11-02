using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.PostgreSQL;

public static class ServiceCollectionExtensions
{
    public static void AddPostgreSQL(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, PostgreSQLQueryExecutor>();
        serviceCollection.AddSingleton<PostgreSQLConnectionFactory>();
    }
}
