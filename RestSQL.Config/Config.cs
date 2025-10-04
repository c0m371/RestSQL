namespace RestSQL.Config;

public record Config(
    IList<Connection> Connections,
    IList<Endpoint> Endpoints
);
