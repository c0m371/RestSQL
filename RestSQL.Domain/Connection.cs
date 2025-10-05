namespace RestSQL.Domain;

public record Connection
{
    public required DatabaseType Type { get; init; }
    public required string ConnectionString { get; init; }
}