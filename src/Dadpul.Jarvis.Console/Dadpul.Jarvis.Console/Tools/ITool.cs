using System.Text.Json.Nodes;

namespace Dadpul.Jarvis.Console.Tools;

internal interface ITool
{
    string Name { get; }

    string Description { get; }

    JsonObject Parameters { get; }

    Task<ToolResult> ExecuteAsync(
        JsonObject arguments,
        CancellationToken cancellationToken);
}