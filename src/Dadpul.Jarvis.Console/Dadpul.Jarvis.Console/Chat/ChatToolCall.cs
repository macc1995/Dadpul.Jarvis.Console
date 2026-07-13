using System.Text.Json.Nodes;

namespace Dadpul.Jarvis.Console.Chat;

internal sealed class ChatToolCall
{
    public required string Name { get; init; }

    public required JsonObject Arguments { get; init; }
}