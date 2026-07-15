namespace Dadpul.Jarvis.Docker.Contracts;

public sealed record DockerContainerInfo(
    string Id,
    IReadOnlyList<string> Names,
    string Image,
    string State,
    string Status,
    IReadOnlyDictionary<string, string> Labels);
