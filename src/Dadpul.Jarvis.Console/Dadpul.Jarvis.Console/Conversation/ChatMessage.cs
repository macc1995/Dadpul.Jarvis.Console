// Bonjour

namespace Dadpul.Jarvis.Console.Conversation
{
   using Dadpul.Jarvis.Console.Chat;

   internal sealed record ChatMessage(ChatRole Role, string Content, IReadOnlyList<ChatToolCall>? ToolCalls = null, string? ToolName = null);
}