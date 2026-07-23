// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Contracts;

public sealed record DockerActionResult(bool Success, string Node, string ContainerId, string ContainerName, string Message);