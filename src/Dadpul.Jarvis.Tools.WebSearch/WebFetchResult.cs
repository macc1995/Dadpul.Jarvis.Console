// Bonjour

namespace Dadpul.Jarvis.Tools.WebSearch;

public sealed record WebFetchResult(string RequestedUrl, string FinalUrl, string? Title, string ContentType, string Content, bool Truncated);