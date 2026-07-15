// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDockerController.cs" company="KUKA Deutschland GmbH">
//   Copyright (c) KUKA Deutschland GmbH 2006 - 2026
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Dadpul.Jarvis.Docker.Contracts;

public interface IDockerController
{
   #region Public Properties

   IReadOnlyCollection<string> NodeNames { get; }

   #endregion

   #region Public Methods and Operators

   Task<DockerActionResult> RestartContainerAsync(string node, string containerReference, CancellationToken cancellationToken);

   Task<DockerContainerSearchResult> SearchContainersAsync(string? node, string? query, bool includeStopped, CancellationToken cancellationToken);

   Task<DockerActionResult> StartContainerAsync(string node, string containerName, CancellationToken cancellationToken);

   Task<DockerActionResult> StopContainerAsync(string node, string containerName, CancellationToken cancellationToken);

   #endregion
}