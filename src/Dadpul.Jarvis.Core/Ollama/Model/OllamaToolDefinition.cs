// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaToolDefinition
{
   #region Public Properties

   [JsonPropertyName("function")] public required OllamaToolFunction Function { get; init; }

   [JsonPropertyName("type")] public string Type { get; init; } = "function";

   #endregion
}