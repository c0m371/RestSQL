namespace RestSQL.Domain;

public record Authentication
{
    public required string Authority { get; init; }
    public required string Audience { get; init; }
    public IList<string> Scopes { get; init; } = [];
}