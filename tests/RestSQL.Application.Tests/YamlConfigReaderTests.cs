using Microsoft.Extensions.Logging;
using Moq;
using RestSQL.Application; // Make sure this using directive is present
using Xunit;
using System;
using System.IO;
using System.Linq;

namespace RestSQL.Application.Tests;

public class YamlConfigReaderTests
{
    private readonly Mock<ILogger<YamlConfigReader>> _loggerMock = new Mock<ILogger<YamlConfigReader>>();

    // Helper property for consistent YAML
    private string BaseYaml1 => @"
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
    private string BaseYaml2 => @"
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

    private string AuthYaml => @"
authentication:
  authority: authority
  audience: audience  
";

    // --- Original Tests (Slightly refactored for clarity) ---

    [Fact]
    public void Read_ThrowsArgumentException_WhenDirectoryDoesNotExist()
    {
        var reader = new YamlConfigReader(_loggerMock.Object);
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
            var reader = new YamlConfigReader(_loggerMock.Object);
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
            File.WriteAllText(Path.Combine(tempDir, "a.yaml"), BaseYaml1);
            File.WriteAllText(Path.Combine(tempDir, "b.yaml"), BaseYaml2);

            var reader = new YamlConfigReader(_loggerMock.Object);
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

    [Fact]
    public void Read_ThrowsInvalidOperationException_WhenMultipleAuth()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "a.yaml"), AuthYaml);
            File.WriteAllText(Path.Combine(tempDir, "b.yaml"), AuthYaml);

            var reader = new YamlConfigReader(_loggerMock.Object);
            var ex = Assert.Throws<InvalidOperationException>(() => reader.Read(tempDir));
            Assert.Contains("Only a single authentication config should be provided", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Read_ReturnsAuthConfig_WhenOnlyOne()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "a.yaml"), AuthYaml);

            var reader = new YamlConfigReader(_loggerMock.Object);
            var config = reader.Read(tempDir);

            Assert.NotNull(config.Authentication);
            Assert.Equal("audience", config.Authentication.Audience);
            Assert.Equal("authority", config.Authentication.Authority);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Read_ReturnsCachedConfig_OnSecondCall()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var fileAPath = Path.Combine(tempDir, "a.yaml");
        var fileBPath = Path.Combine(tempDir, "b.yaml");

        try
        {
            // ARRANGE: Write files for initial load
            File.WriteAllText(fileAPath, BaseYaml1);
            File.WriteAllText(fileBPath, BaseYaml2);

            var reader = new YamlConfigReader(_loggerMock.Object);

            // 1. ACT: First read (loads from disk and caches)
            var config1 = reader.Read(tempDir);

            // ASSERT: Verify initial load was correct
            Assert.Equal(2, config1.Connections.Count);
            Assert.Contains(config1.Endpoints, e => e.Path == "/test1");

            // 2. ARRANGE: Delete all files on disk
            File.Delete(fileAPath);
            File.Delete(fileBPath);
            Assert.False(Directory.GetFiles(tempDir, "*.yaml").Any());

            // 3. ACT: Second read (should retrieve from cache, ignoring the missing files)
            var config2 = reader.Read(tempDir);

            // ASSERT: Verify the second call returns the same, valid (cached) data.
            // If caching failed, this call would throw an ArgumentException because the directory is now empty of yaml files.
            Assert.Equal(config1.Connections.Count, config2.Connections.Count);
            Assert.True(config2.Connections.ContainsKey("conn1"));
            Assert.True(config2.Connections.ContainsKey("conn2"));
            Assert.Contains(config2.Endpoints, e => e.Path == "/test1");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Read_LoadsConfigOnlyOnce_WhenCalledConcurrently()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // ARRANGE: Use a special YAML that we can track load counts with
            File.WriteAllText(Path.Combine(tempDir, "load_count_test.yaml"), BaseYaml1);

            // We will use the logger to prove that the "Loading and merging files" log message 
            // is only written once, which proves the lock prevented concurrent loading.

            var reader = new YamlConfigReader(_loggerMock.Object);

            // ACT: Start many concurrent tasks trying to read the same config path
            var taskCount = 50;
            var tasks = Enumerable.Range(0, taskCount)
                .Select(_ => Task.Run(() => reader.Read(tempDir)))
                .ToList();

            await Task.WhenAll(tasks);

            // ASSERT: Verify all tasks succeeded
            Assert.All(tasks, t => Assert.NotNull(t.Result));

            // ASSERT: Verify the "Loading" log was only written once
            // This is the best way to prove the lock and double-check logic worked.
            // The log line we are tracking is: "Config not found in cache. Loading and merging files from: {path}"

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading and merging files from:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once); // <--- Key assertion for thread safety and locking

        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}