using RestSQL.Domain;

namespace RestSQL.Application.Interfaces;

public interface IYamlConfigReader
{
    Config Read(string path);
}
