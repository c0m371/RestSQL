namespace RestSQL.Domain;

public record Endpoint(
    string Path,
    string Verb,
    int StatusCode,
    Dictionary<string, SqlQuery> SqlQueries, 
    OutputField OutputStructure
);
