using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace RestSQL.IntegrationTests.SqlServer
{
    public class SqlServerFixture : IDatabaseFixture
    {
        private readonly MsSqlContainer container;

        public SqlServerFixture()
        {
            container = new MsSqlBuilder().Build();
        }

        public string ConnectionString => container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await container.StartAsync();

            await using var conn = new SqlConnection(container.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE dbo.Users (
    Username nvarchar(255) NOT NULL PRIMARY KEY,
    FirstName nvarchar(255) NOT NULL,
    LastName nvarchar(255) NOT NULL
);

CREATE TABLE dbo.Posts (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username nvarchar(255) NOT NULL,
    Title nvarchar(1000) NOT NULL,
    Description nvarchar(max) NULL,
    CreationDate datetimeoffset NOT NULL
);

CREATE TABLE dbo.Tags (
    PostId int NOT NULL,
    Tag nvarchar(255) NOT NULL,
    CONSTRAINT Tags_PK PRIMARY KEY (PostId, Tag)
);

INSERT INTO dbo.Users (Username, FirstName, LastName) VALUES
('alice_codes', 'Alice', 'Smith'),
('bob_devs', 'Robert', 'Jones');
";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            try
            {
                await container.StopAsync();
                await container.DisposeAsync();
            }
            catch { /* best-effort cleanup */ }
        }
    }
}
