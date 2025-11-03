using System;

namespace RestSQL.IntegrationTests.MySql;

public class MySqlTests(MySqlFixture fixture) : PostsTestsBase<MySqlFixture>(fixture)
{
    protected override string YamlFolder => Path.Combine(AppContext.BaseDirectory, "MySql", "Yaml");
}
