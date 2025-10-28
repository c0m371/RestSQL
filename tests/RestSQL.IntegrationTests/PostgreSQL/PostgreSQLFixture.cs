using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;

namespace RestSQL.IntegrationTests.PostgreSQL;

public class PostgreSQLFixture : IDatabaseFixture
{
    private readonly PostgreSqlContainer container;


    public PostgreSQLFixture()
    {
        container = new PostgreSqlBuilder().Build();
    }

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var conn = new NpgsqlConnection(container.GetConnectionString());
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE public.users (
	username varchar NOT NULL,
	first_name varchar NOT NULL,
	last_name varchar NOT NULL,
	CONSTRAINT user_pk PRIMARY KEY (username)
);

-- Note: FK to users missing
CREATE TABLE public.posts (
	id int4 GENERATED ALWAYS AS IDENTITY NOT NULL,
	username varchar not null,
	title varchar NOT NULL,
	description varchar NULL,
	creation_date timestamptz NOT NULL,
	CONSTRAINT post_pk PRIMARY KEY (id)
);

-- Note: FK to posts missing
CREATE TABLE public.tags (
	post_id int4 NOT NULL,
	tag varchar NOT NULL,
	CONSTRAINT tags_pk PRIMARY KEY (post_id, tag)
);

INSERT INTO public.users (username, first_name, last_name) VALUES
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