// Bonjour

namespace Dadpul.Jarvis.Console.Ollama.Model;

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

internal sealed class OllamaToolDefinition
{
   #region Public Properties

   [JsonPropertyName("function")] public required OllamaToolFunction Function { get; init; }

   [JsonPropertyName("type")] public string Type { get; init; } = "function";

   #endregion
}

internal sealed class OllamaToolFunction
{
   #region Public Properties

   [JsonPropertyName("description")] public required string Description { get; init; }

   [JsonPropertyName("name")] public required string Name { get; init; }

   [JsonPropertyName("parameters")] public required JsonObject Parameters { get; init; }

   #endregion
}