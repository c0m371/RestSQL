namespace RestSQL.Domain;

public record SqlQuery(
    string ConnectionName,
    string Sql
);