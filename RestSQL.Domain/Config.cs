namespace RestSQL.Domain;

public record Config
{
    public required IDictionary<string, Connection> Connections { get;  init; }
    public required IList<Endpoint> Endpoints { get; init; }
}
