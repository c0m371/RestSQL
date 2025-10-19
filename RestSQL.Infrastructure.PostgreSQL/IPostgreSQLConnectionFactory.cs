using System.Data;

namespace RestSQL.Infrastructure.PostgreSQL;

public interface IPostgreSQLConnectionFactory
{
    IDbConnection CreatePostgreSQLConnection(string connectionString);
}
