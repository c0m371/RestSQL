using RestSQL.Application;
using RestSQL.Infrastructure;
using RestSQL.Infrastructure.Dapper;
using RestSQL.Infrastructure.MySql;
using RestSQL.Infrastructure.PostgreSQL;
using RestSQL.Infrastructure.SqlServer;
using RestSQL.Infrastructure.Oracle;

namespace RestSQL;

public static class ServiceCollectionExtensions
{
    public static void AddRestSQL(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddRestSQLApplication();
        serviceCollection.AddRestSQLInfrastructure();
        serviceCollection.AddRestSQLInfrastructureDapper();
        serviceCollection.AddPostgreSQL();
        serviceCollection.AddSqlServer();
        serviceCollection.AddMySql();
        serviceCollection.AddOracle();
    }
}
