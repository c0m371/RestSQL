using Microsoft.Extensions.DependencyInjection;
using RestSQL.Data.QueryExecution;
using RestSQL.Data.PostgreSQL;
using RestSQL.Data.Interfaces;

namespace RestSQL.Data;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, PostgreSQLQueryExecutor>();
        serviceCollection.AddSingleton<IQueryDispatcher, QueryDispatcher>();
    }
}
