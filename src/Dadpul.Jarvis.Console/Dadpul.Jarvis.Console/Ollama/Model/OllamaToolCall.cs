using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Dadpul.Jarvis.Console.Ollama.Model;

internal sealed class OllamaToolCall
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public required OllamaCalledFunction Function { get; init; }
}

internal sealed class OllamaCalledFunction
{
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("arguments")]
    public JsonObject Arguments { get; init; } = new();
}