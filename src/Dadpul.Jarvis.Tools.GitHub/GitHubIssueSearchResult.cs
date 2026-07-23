// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal sealed record GitHubIssueSearchResult(string Repository, IReadOnlyList<GitHubIssueSummary> Issues, int TotalCount, bool IncompleteResults);