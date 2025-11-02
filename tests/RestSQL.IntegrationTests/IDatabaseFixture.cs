namespace RestSQL.IntegrationTests;

public interface IDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }
}