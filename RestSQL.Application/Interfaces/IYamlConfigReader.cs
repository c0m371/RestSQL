using RestSQL.Domain;

namespace RestSQL.Application.Interfaces;

public interface IYamlConfigReader
{
    Task<Config> ReadAsync(string path);
}
