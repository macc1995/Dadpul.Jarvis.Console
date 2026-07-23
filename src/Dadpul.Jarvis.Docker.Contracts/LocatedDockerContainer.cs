// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Contracts;

public sealed record LocatedDockerContainer(string Node, DockerContainerInfo Container);