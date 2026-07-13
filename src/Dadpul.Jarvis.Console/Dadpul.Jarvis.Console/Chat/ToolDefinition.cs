using System.Text.Json.Nodes;

namespace Dadpul.Jarvis.Console.Chat;

internal sealed class ChatToolDefinition
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required JsonObject Parameters { get; init; }
}