// Made by Dadpul

namespace Dadpul.Jarvis.Console.Chat;

internal sealed class ChatResponseChunk
{
   #region Public Properties

   public string Content { get; init; } = string.Empty;

   public bool Done { get; init; }

   public ChatMetrics? Metrics { get; init; }

   public IReadOnlyList<ChatToolCall> ToolCalls { get; init; } = Array.Empty<ChatToolCall>();

   #endregion
}