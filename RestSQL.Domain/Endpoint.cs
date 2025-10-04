namespace RestSQL.Domain;

public record Endpoint(
    string Path,
    Dictionary<string, UrlParameter> UrlParameters, 
    Dictionary<string, SqlQuery> SqlQueries, 
    OutputField OutputStructure
);
