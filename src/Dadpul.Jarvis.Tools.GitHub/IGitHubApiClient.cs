// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal interface IGitHubApiClient
{
   #region Public Properties

   string DefaultRepository { get; }

   #endregion

   #region Public Methods and Operators

   Task<T> GetAsync<T>(string? repository, string relativePath, CancellationToken cancellationToken);

   string ResolveRepository(string? repository);

   #endregion
}
