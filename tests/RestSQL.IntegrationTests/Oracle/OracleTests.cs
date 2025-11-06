namespace RestSQL.IntegrationTests.Oracle;

[Collection("Integration")]
public class OracleTests(OracleFixture fixture) : PostsTestsBase<OracleFixture>(fixture)
{
    protected override string YamlFolder => Path.Combine(AppContext.BaseDirectory, "Oracle", "Yaml");
}