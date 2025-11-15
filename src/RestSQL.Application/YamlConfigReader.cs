using Microsoft.Extensions.Logging;
using RestSQL.Application.Interfaces;
using RestSQL.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Concurrent;

namespace RestSQL.Application;

public class YamlConfigReader(ILogger<YamlConfigReader> logger) : IYamlConfigReader
{
    private readonly ConcurrentDictionary<string, Config> configCache = new();
    private readonly object cacheLock = new();

    public Config Read(string path)
    {
        logger.LogDebug("Attempting to read or retrieve config from cache for path: {path}", path);

        if (configCache.TryGetValue(path, out var cachedConfig))
        {
            logger.LogDebug("Config found in cache for path: {path}", path);
            return cachedConfig;
        }

        lock (cacheLock)
        {
            if (configCache.TryGetValue(path, out cachedConfig))
            {
                logger.LogDebug("Config found in cache after lock for path: {path}", path);
                return cachedConfig;
            }

            logger.LogInformation("Config not found in cache. Loading and merging files from: {path}", path);

            var mergedConfig = ReadAndMergeFiles(path);

            configCache.TryAdd(path, mergedConfig);
            logger.LogInformation("Config successfully loaded and cached for path: {path}", path);

            return mergedConfig;
        }
    }

    private Config ReadAndMergeFiles(string path)
    {
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

        var authConfigs = allConfigs
            .Where(c => c.Authentication != null)
            .Select(c => c.Authentication)
            .ToList();
            
        if (authConfigs.Count > 1)
            throw new InvalidOperationException("Only a single authentication config should be provided");

        var authConfig = authConfigs.SingleOrDefault();

        var mergedConfig = new Config
        {
            Connections = allConfigs.SelectMany(c => c.Connections).ToDictionary(),
            Endpoints = [.. allConfigs.SelectMany(c => c.Endpoints)],
            Authentication = authConfig
        };

        logger.LogDebug("Merged config: {connections} connections, {endpoints} endpoints",
            mergedConfig.Connections.Count, mergedConfig.Endpoints.Count);

        return mergedConfig;
    }
}