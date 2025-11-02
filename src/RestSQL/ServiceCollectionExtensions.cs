using RestSQL.Application;
using RestSQL.Infrastructure;
using RestSQL.Infrastructure.PostgreSQL;
using RestSQL.Infrastructure.SqlServer;

namespace RestSQL;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQL(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddRestSQLInfrastructure();
        serviceCollection.AddPostgreSQL();
        serviceCollection.AddSqlServer();
        serviceCollection.AddRestSQLApplication();
    }
}
