// Made by Dadpul

namespace Dadpul.Jarvis.Core.Conversation
{
   using Dadpul.Jarvis.Core.Chat;

   public sealed record ChatMessage(ChatRole Role, string Content, IReadOnlyList<ChatToolCall>? ToolCalls = null, string? ToolName = null);
}