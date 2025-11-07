using Microsoft.Extensions.Logging;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RestSQL.Application;

public class YamlConfigReader(ILogger<YamlConfigReader> logger) : IYamlConfigReader
{
    public Config Read(string path)
    {
        logger.LogDebug("Reading YAML config from path: {path}", path);

        if (!Directory.Exists(path))
            throw new ArgumentException($"Path {path} does not exist", nameof(path));

        var files = Directory.GetFiles(path, "*.yaml").ToList();
        logger.LogDebug("Found {count} yaml files in {path}", files.Count, path);

        if (files.Count == 0)
            throw new ArgumentException($"Directory {path} doesn't contain yaml files", nameof(path));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var allConfigs = files.Select(file =>
        {
            logger.LogDebug("Deserializing yaml file {file}", file);
            using var reader = new StreamReader(file);
            return deserializer.Deserialize<Config>(reader);
        });

        var mergedConfig = new Config
        {
            Connections = allConfigs.SelectMany(c => c.Connections).ToDictionary(),
            Endpoints = [.. allConfigs.SelectMany(c => c.Endpoints)]
        };

        logger.LogDebug("Merged config: {connections} connections, {endpoints} endpoints",
            mergedConfig.Connections.Count, mergedConfig.Endpoints.Count);

        return mergedConfig;
    }
}
