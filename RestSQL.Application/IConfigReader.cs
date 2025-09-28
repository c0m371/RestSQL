using System;

namespace RestSQL.Application;

public interface IConfigReader
{
    Task<Config.Config> ReadAsync();
}
