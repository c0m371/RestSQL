using System.Text.Json;

namespace RestSQL.IntegrationTests;

public abstract class PostsTestsBase<TFixture> : IntegrationTestBase<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected PostsTestsBase(TFixture fixture) : base(fixture) { }

    protected abstract override string YamlFolder { get; }

    [Fact]
    public async Task PostsApi_ReturnsCreatedPosts()
    {
        await CreatePost("The Joys of Async/Await", "A deep dive into non-blocking operations in C#.", new DateTime(2025, 10, 25, 15, 3, 4), "alice_codes", "C#", "Async");
        await CreatePost("PostgreSQL vs MySQL: A Performance Review", "Comparing the speed and features of two popular databases.", new DateTime(2025, 10, 26, 16, 5, 15), "bob_devs", "PostgreSQL", "Database");

        var client = Client ?? throw new Xunit.Sdk.XunitException("Client not initialized");
        var resp = await client.GetAsync("/api/posts");
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        Assert.Equal("The Joys of Async/Await", doc.RootElement[0].GetProperty("title").GetString());
        Assert.Equal("A deep dive into non-blocking operations in C#.", doc.RootElement[0].GetProperty("description").GetString());
        Assert.Equal("alice_codes", doc.RootElement[0].GetProperty("username").GetString());
        Assert.Equal("2025-10-25 15:03:04", doc.RootElement[0].GetProperty("creationDate").GetString());
        Assert.Equal(JsonValueKind.Array, doc.RootElement[0].GetProperty("tags").ValueKind);
        Assert.Equal(2, doc.RootElement[0].GetProperty("tags").GetArrayLength());
        Assert.Equal("C#", doc.RootElement[0].GetProperty("tags")[0].ToString());
        Assert.Equal("Async", doc.RootElement[0].GetProperty("tags")[1].ToString());

        Assert.Equal("PostgreSQL vs MySQL: A Performance Review", doc.RootElement[1].GetProperty("title").GetString());
        Assert.Equal("Comparing the speed and features of two popular databases.", doc.RootElement[1].GetProperty("description").GetString());
        Assert.Equal("bob_devs", doc.RootElement[1].GetProperty("username").GetString());
        Assert.Equal("2025-10-26 16:05:15", doc.RootElement[1].GetProperty("creationDate").GetString());
        Assert.Equal(JsonValueKind.Array, doc.RootElement[1].GetProperty("tags").ValueKind);
        Assert.Equal(2, doc.RootElement[1].GetProperty("tags").GetArrayLength());
        Assert.Equal("PostgreSQL", doc.RootElement[1].GetProperty("tags")[0].ToString());
        Assert.Equal("Database", doc.RootElement[1].GetProperty("tags")[1].ToString());
    }

    protected async Task CreatePost(string title, string description, DateTime creationDate, string username, params string[] tags)
    {
        var client = Client ?? throw new Xunit.Sdk.XunitException("Client not initialized");

        using var post = CreateStringContent(new { title, description, creationDate, username });
        var resp = await client.PostAsync("/api/posts", post);
        resp.EnsureSuccessStatusCode();
        var id = await GetCreatedPostId(resp);

        foreach (var tag in tags)
        {
            using var content = CreateStringContent(tag);
            resp = await client.PostAsync($"/api/posts/{id}/tags", content);
            resp.EnsureSuccessStatusCode();
        }
    }

    private static async Task<long> GetCreatedPostId(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var id = doc.RootElement.GetProperty("id").GetInt64();
        return id;
    }

    private static StringContent CreateStringContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value));
    }
}