using System;
using Microsoft.Extensions.DependencyInjection;
using RestSQL.Data.Interfaces;
using RestSQL.Data.PostgreSQL;

namespace RestSQL.Data;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IQueryExecutor, PostgreSQLQueryExecutor>();
    }
}
