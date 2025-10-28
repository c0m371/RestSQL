using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RestSQL.IntegrationTests;

public abstract class IntegrationTestBase<TFixture> : IAsyncLifetime, IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    private WebApplicationFactory<Program>? factory;
    private readonly string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    protected IntegrationTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    protected TFixture Fixture { get; init; }
    protected abstract string YamlFolder { get; }
    protected HttpClient? Client => factory?.CreateClient();

    public virtual Task InitializeAsync()
    {
        PrepareYamlFiles();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(GetConfigurationOverrides());
            });
        });

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        try
        {
            factory?.Dispose();
            await Task.Yield();
        }
        catch
        {
            try { Directory.Delete(tempFolder, true); } catch { /* best-effort cleanup */ }
        }
    }

    protected virtual IDictionary<string, string?> GetConfigurationOverrides()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RestSQL:ConfigFolder"] = tempFolder
        };

        return overrides;
    }

    protected static async Task<T?> GetJsonAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    protected async Task<T?> GetJsonAsync<T>(string path)
    {
        if (Client == null) throw new InvalidOperationException("Client not created. Ensure InitializeAsync ran.");
        using var resp = await Client.GetAsync(path).ConfigureAwait(false);
        return await GetJsonAsync<T>(resp).ConfigureAwait(false);
    }

    private void PrepareYamlFiles()
    {
        Directory.CreateDirectory(tempFolder);

        string[] files = System.IO.Directory.GetFiles(YamlFolder);

        if (!files.Any(f => f.EndsWith("connections.yaml")))
            throw new Exception("connections.yaml file expected");

        foreach (string s in files)
        {
            var fileName = Path.GetFileName(s);
            var destFile = Path.Combine(tempFolder, fileName);
            File.Copy(s, destFile, true);
        }

        var connectionsFile = Path.Combine(tempFolder, "connections.yaml");
        string text = File.ReadAllText(connectionsFile);
        text = text.Replace("<connectionString>", Fixture.ConnectionString);
        File.WriteAllText(connectionsFile, text);
    }
}