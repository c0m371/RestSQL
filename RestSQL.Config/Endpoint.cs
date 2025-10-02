namespace RestSQL.Config;

public record Endpoint(
    string Path,
    string ConnectionName,
    string Sql,
    Dictionary<string, object> Parameters,
    OutputField OutputStructure
);
