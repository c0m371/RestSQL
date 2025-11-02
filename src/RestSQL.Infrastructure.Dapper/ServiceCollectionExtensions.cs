using System;
using Microsoft.Extensions.DependencyInjection;
using RestSQL.Infrastructure.Interfaces;

namespace RestSQL.Infrastructure.Dapper;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLInfrastructureDapper(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDataAccess, DapperDataAccess>();
    }
}
