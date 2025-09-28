namespace RestSQL.Config;

public record Endpoint(
    string Path,
    string Sql,
    Dictionary<string, object> Parameters
);
