// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal sealed record GitHubRepositoryInfo(
   string FullName,
   string Owner,
   string DefaultBranch,
   string Visibility,
   bool Private,
   bool Archived,
   string? Description,
   string Url);