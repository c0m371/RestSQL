namespace RestSQL.Domain;

public record Connection(
    string Name,
    DatabaseType Type,
    string ConnectionString
);
