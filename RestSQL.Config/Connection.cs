using System;

namespace RestSQL.Config;

public record Connection(string Name, DatabaseType Type, string ConnectionString);
