using Dadpul.Jarvis.Console.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dadpul.Jarvis.Console.Conversation
{
    internal sealed record ChatMessage(
    ChatRole Role,
    string Content,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    string? ToolName = null);
}
