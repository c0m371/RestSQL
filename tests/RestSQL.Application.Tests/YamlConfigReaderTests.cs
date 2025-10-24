namespace RestSQL.Application.Tests;

public class YamlConfigReaderTests
{
    [Fact]
    public void Read_ThrowsArgumentException_WhenDirectoryDoesNotExist()
    {
        var reader = new YamlConfigReader();
        var path = Guid.NewGuid().ToString(); // unlikely to exist

        var ex = Assert.Throws<ArgumentException>(() => reader.Read(path));
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void Read_ThrowsArgumentException_WhenNoYamlFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var reader = new YamlConfigReader();
            var ex = Assert.Throws<ArgumentException>(() => reader.Read(tempDir));
            Assert.Contains("doesn't contain yaml files", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Read_ReturnsMergedConfig_WhenYamlFilesExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var yaml1 = @"
connections:
  conn1:
    connectionString: ""Data Source=1""
endpoints:
  - path: ""/test1""
    method: ""GET""
    statusCode: 200
    sqlQueries: {}
    outputStructure: {}
";
            var yaml2 = @"
connections:
  conn2:
    connectionString: ""Data Source=2""
endpoints:
  - path: ""/test2""
    method: ""POST""
    statusCode: 201
    sqlQueries: {}
    outputStructure: {}
";
            File.WriteAllText(Path.Combine(tempDir, "a.yaml"), yaml1);
            File.WriteAllText(Path.Combine(tempDir, "b.yaml"), yaml2);

            var reader = new YamlConfigReader();
            var config = reader.Read(tempDir);

            Assert.True(config.Connections.ContainsKey("conn1"));
            Assert.True(config.Connections.ContainsKey("conn2"));
            Assert.Contains(config.Endpoints, e => e.Path == "/test1");
            Assert.Contains(config.Endpoints, e => e.Path == "/test2");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}