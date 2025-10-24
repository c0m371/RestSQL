namespace RestSQL.Domain;

public record Config
{
    public required IDictionary<string, Connection> Connections { get; init; } = new Dictionary<string, Connection>();
    public required IList<Endpoint> Endpoints { get; init; } = [];
}
