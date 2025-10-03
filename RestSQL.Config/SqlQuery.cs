namespace RestSQL.Config;

public record SqlQuery(
    string ConnectionName,
    string Sql
);