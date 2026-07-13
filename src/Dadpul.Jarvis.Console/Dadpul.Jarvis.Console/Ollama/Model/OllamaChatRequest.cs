using Dadpul.Jarvis.Console.Ollama.Models;
using System.Text.Json.Serialization;

namespace Dadpul.Jarvis.Console.Ollama.Model;

internal sealed class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }
    [JsonPropertyName("tools")]
    public IReadOnlyList<OllamaToolDefinition>? Tools { get; init; }
    [JsonPropertyName("messages")]
    public required IReadOnlyList<OllamaChatMessage> Messages { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }
}