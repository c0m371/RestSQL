namespace RestSQL.IntegrationTests.SQLite;

public class SQLiteTests(SQLiteFixture fixture) : PostsTestsBase<SQLiteFixture>(fixture)
{
    protected override string YamlFolder => Path.Combine(AppContext.BaseDirectory, "SQLite", "Yaml");
}