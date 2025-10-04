namespace RestSQL.Domain;

public record Config(
    IList<Connection> Connections,
    IList<Endpoint> Endpoints
);
