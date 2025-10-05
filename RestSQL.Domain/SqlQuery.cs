namespace RestSQL.Domain;

public record SqlQuery
{
    public required string ConnectionName { get; set; }
    public required string Sql { get; set; }
}