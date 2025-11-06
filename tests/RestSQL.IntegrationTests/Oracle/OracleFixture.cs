using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;

namespace RestSQL.IntegrationTests.Oracle;

public class OracleFixture : IDatabaseFixture
{
    private readonly OracleContainer container;

    public OracleFixture()
    {
        container = new OracleBuilder()
            .Build();
    }

    public string ConnectionString => container.GetConnectionString();



    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var conn = new OracleConnection(container.GetConnectionString());
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();

        // Create schema using Oracle syntax
        cmd.CommandText = @"
BEGIN
    BEGIN
        EXECUTE IMMEDIATE 'DROP TABLE tags';
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;

    BEGIN
        EXECUTE IMMEDIATE 'DROP TABLE posts';
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;

    BEGIN
        EXECUTE IMMEDIATE 'DROP TABLE users';
    EXCEPTION
        WHEN OTHERS THEN NULL;
    END;

    EXECUTE IMMEDIATE 'CREATE SEQUENCE posts_seq start with 1 increment by 1 nocache nocycle';

    EXECUTE IMMEDIATE 'CREATE TABLE users (
        username VARCHAR2(100) NOT NULL PRIMARY KEY,
        first_name VARCHAR2(100) NOT NULL,
        last_name VARCHAR2(100) NOT NULL
    )';

    EXECUTE IMMEDIATE 'CREATE TABLE posts (
        id NUMBER PRIMARY KEY,
        username VARCHAR2(100) NOT NULL,
        title VARCHAR2(255) NOT NULL,
        description CLOB,
        creation_date TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
    )';

    EXECUTE IMMEDIATE 'CREATE TABLE tags (
        post_id NUMBER NOT NULL,
        tag VARCHAR2(100) NOT NULL,
        CONSTRAINT pk_tags PRIMARY KEY (post_id, tag)
    )';

    EXECUTE IMMEDIATE 'CREATE INDEX idx_posts_username ON posts(username)';
    EXECUTE IMMEDIATE 'CREATE INDEX idx_tags_post_id ON tags(post_id)';
END;
";

        await cmd.ExecuteNonQueryAsync();

        cmd = conn.CreateCommand();

        // Create schema using Oracle syntax
        cmd.CommandText = @"
BEGIN
    INSERT INTO users (username, first_name, last_name) 
    VALUES ('alice_codes', 'Alice', 'Smith');

    INSERT INTO users (username, first_name, last_name) 
    VALUES ('bob_devs', 'Robert', 'Jones');

    COMMIT;
END;
";

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await container.DisposeAsync();
        }
        catch { /* best-effort cleanup */ }
    }
}