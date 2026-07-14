// Bonjour

namespace Dadpul.Jarvis.Tools.WebSearch;

public sealed record WebSearchResult(string Title, string Url, string Snippet, IReadOnlyList<string> Engines, string? PublishedDate);