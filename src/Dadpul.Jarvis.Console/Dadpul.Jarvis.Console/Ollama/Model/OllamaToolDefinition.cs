using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Dadpul.Jarvis.Console.Ollama.Models;

internal sealed class OllamaToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public required OllamaToolFunction Function { get; init; }
}

internal sealed class OllamaToolFunction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("parameters")]
    public required JsonObject Parameters { get; init; }
}