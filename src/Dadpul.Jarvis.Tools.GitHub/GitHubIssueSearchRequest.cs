// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal sealed record GitHubIssueSearchRequest(
   string? Repository,
   string? Query,
   string State,
   IReadOnlyList<string> Labels,
   string? Assignee,
   int Limit,
   string Sort,
   string Order);

internal sealed record GitHubIssueSummary(
   int Number,
   string Title,
   string State,
   IReadOnlyList<string> Labels,
   DateTimeOffset UpdatedAt,
   string Url,
   string Summary);

internal sealed record GitHubIssueSearchResult(
   string Repository,
   IReadOnlyList<GitHubIssueSummary> Issues,
   int TotalCount,
   bool IncompleteResults);
