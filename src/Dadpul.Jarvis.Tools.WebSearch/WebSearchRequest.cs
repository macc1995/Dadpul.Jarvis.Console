// Bonjour

namespace Dadpul.Jarvis.Tools.WebSearch;

public sealed record WebSearchRequest(string Query, int MaxResults, string Language, string? TimeRange);