using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RestSQL.Application;

public class YamlConfigReader : IYamlConfigReader
{
    private readonly ILogger<YamlConfigReader> _logger;

    public YamlConfigReader(ILogger<YamlConfigReader> logger)
    {
        _logger = logger;
    }

    public YamlConfigReader() : this(NullLogger<YamlConfigReader>.Instance)
    {
    }

    public Config Read(string path)
    {
        _logger.LogInformation("Reading YAML config from path: {path}", path);

        if (!Directory.Exists(path))
            throw new ArgumentException($"Path {path} does not exist", nameof(path));

        var files = Directory.GetFiles(path, "*.yaml").ToList();
        _logger.LogDebug("Found {count} yaml files in {path}", files.Count, path);

        if (files.Count == 0)
            throw new ArgumentException($"Directory {path} doesn't contain yaml files", nameof(path));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var allConfigs = files.Select(file =>
        {
            _logger.LogDebug("Deserializing yaml file {file}", file);
            using var reader = new StreamReader(file);
            return deserializer.Deserialize<Config>(reader);
        });

        var mergedConfig = new Config
        {
            Connections = allConfigs.SelectMany(c => c.Connections).ToDictionary(),
            Endpoints = [.. allConfigs.SelectMany(c => c.Endpoints)]
        };

        _logger.LogInformation("Merged config: {connections} connections, {endpoints} endpoints",
            mergedConfig.Connections.Count, mergedConfig.Endpoints.Count);

        return mergedConfig;
    }
}
