namespace RestSQL.IntegrationTests.PostgreSQL;

public class PostgreSQLTests(PostgreSQLFixture fixture) : PostsTestsBase<PostgreSQLFixture>(fixture)
{
    protected override string YamlFolder => Path.Combine(AppContext.BaseDirectory, "PostgreSQL", "Yaml");
}
