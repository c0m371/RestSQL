using System;
using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Sqlite;

public static class ServiceCollectionExtensions
{
    public static void AddSqlite(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, SqliteQueryExecutor>();
        serviceCollection.AddSingleton<SqliteConnectionFactory>();
    }
}
