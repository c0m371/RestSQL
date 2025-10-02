using System;
using RestSQL.Application.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RestSQL.Application;

public class YamlConfigReader : IYamlConfigReader
{
    public Task<Config.Config> ReadAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new ArgumentException($"Path {path} does not exist", nameof(path));

        var files = Directory.GetFiles(path, "*.yaml").ToList();

        if (files.Count == 0)
            throw new ArgumentException($"Directory {path} doesn't contain yaml files", nameof(path));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var allConfigs = files.Select(file =>
        {
            using var reader = new StreamReader(file);
            return deserializer.Deserialize<Config.Config>(reader);
        });

        //TODO validation (no duplicates, ...)

        var mergedConfig = new Config.Config(
            [.. allConfigs.SelectMany(c => c.Connections)],
            [.. allConfigs.SelectMany(c => c.Endpoints)]
        );

        return Task.FromResult(mergedConfig);
    }
}
