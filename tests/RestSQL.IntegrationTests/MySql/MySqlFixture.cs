using MySqlConnector;
using Testcontainers.MySql;

namespace RestSQL.IntegrationTests.MySql;

public class MySqlFixture: IDatabaseFixture
{
    private readonly MySqlContainer container;


    public MySqlFixture()
    {
        container = new MySqlBuilder().Build();
    }

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var conn = new MySqlConnection(container.GetConnectionString());
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS users (
  username VARCHAR(100) NOT NULL,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  PRIMARY KEY (username)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS posts (
  id INT NOT NULL AUTO_INCREMENT,
  username VARCHAR(100) NOT NULL,
  title VARCHAR(255) NOT NULL,
  description TEXT NULL,
  creation_date DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  INDEX idx_posts_username (username)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tags (
  post_id INT NOT NULL,
  tag VARCHAR(100) NOT NULL,
  PRIMARY KEY (post_id, tag),
  INDEX idx_tags_post_id (post_id)
) ENGINE=InnoDB;

INSERT INTO users (username, first_name, last_name) VALUES
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
        catch { /* best-effort */ }
    }
}