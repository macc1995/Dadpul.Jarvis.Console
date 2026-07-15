namespace Dadpul.Jarvis.Docker.Contracts;

public sealed record DockerContainerSearchResult(
    IReadOnlyList<LocatedDockerContainer> Containers,
    IReadOnlyList<DockerNodeFailure> FailedNodes);
