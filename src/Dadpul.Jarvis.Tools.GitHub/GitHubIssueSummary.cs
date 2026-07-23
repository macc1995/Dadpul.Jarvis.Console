// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal sealed record GitHubIssueSummary(
   int Number,
   string Title,
   string State,
   IReadOnlyList<string> Labels,
   DateTimeOffset UpdatedAt,
   string Url,
   string Summary);