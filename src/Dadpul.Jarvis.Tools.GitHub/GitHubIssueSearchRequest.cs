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