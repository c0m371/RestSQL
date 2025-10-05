using RestSQL.Application;
using RestSQL.Infrastructure;
using RestSQL.Infrastructure.PostgreSQL;

namespace RestSQL;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQL(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddRestSQLData();
        serviceCollection.AddPostgreSQLExecutor();
        serviceCollection.AddRestSQLApplication();
    }
}
