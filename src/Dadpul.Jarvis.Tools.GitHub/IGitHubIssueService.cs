// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal interface IGitHubIssueService
{
   #region Public Methods and Operators

   Task<GitHubIssueSearchResult> FindIssuesAsync(GitHubIssueSearchRequest request, CancellationToken cancellationToken);

   #endregion
}