namespace Dadpul.Jarvis.Console.Chat;

internal sealed class ChatResponseChunk
{
    public string Content { get; init; } = string.Empty;
    public IReadOnlyList<ChatToolCall> ToolCalls { get; init; } =
        Array.Empty<ChatToolCall>();
    public bool Done { get; init; }

    public ChatMetrics? Metrics { get; init; }
}