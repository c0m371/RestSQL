using Microsoft.Data.Sqlite;

namespace RestSQL.IntegrationTests.SQLite;

public class SQLiteFixture : IDatabaseFixture
{
    private readonly string dbPath;
    private bool isDisposed;

    public SQLiteFixture()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"restsql_test_{Guid.NewGuid()}.db");
    }

    public string ConnectionString => $"Data Source={dbPath}";

    public async Task InitializeAsync()
    {
        await using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE users (
    username TEXT NOT NULL PRIMARY KEY,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL
);

CREATE TABLE posts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    creation_date TEXT NOT NULL
);

CREATE TABLE tags (
    post_id INTEGER NOT NULL,
    tag TEXT NOT NULL,
    PRIMARY KEY (post_id, tag)
);

INSERT INTO users (username, first_name, last_name) VALUES
('alice_codes', 'Alice', 'Smith'),
('bob_devs', 'Robert', 'Jones');
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    public  Task DisposeAsync()
    {
        if (isDisposed) return Task.CompletedTask;

        try
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
            isDisposed = true;
        }
        catch { /* best-effort */ }

        return Task.CompletedTask;
    }
}