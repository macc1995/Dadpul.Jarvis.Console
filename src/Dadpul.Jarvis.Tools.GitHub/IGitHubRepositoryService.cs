// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal interface IGitHubRepositoryService
{
   #region Public Methods and Operators

   Task<GitHubRepositoryInfo> GetRepositoryAsync(string? repository, CancellationToken cancellationToken);

   #endregion
}