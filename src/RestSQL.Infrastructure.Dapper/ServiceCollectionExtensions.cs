using Microsoft.Extensions.DependencyInjection;

namespace RestSQL.Infrastructure.Dapper;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQLInfrastructureDapper(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDataAccess, DapperDataAccess>();
    }
}
