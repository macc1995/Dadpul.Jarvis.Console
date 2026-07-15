// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using Dadpul.Jarvis.Docker.Contracts;

internal sealed partial class DockerController
{
   private sealed record NodeSearchResult(string Node, IReadOnlyList<DockerContainerInfo> Containers, string? Error);
}