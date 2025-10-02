namespace RestSQL.Application.Interfaces;

public interface IYamlConfigReader
{
    Task<Config.Config> ReadAsync(string path);
}
