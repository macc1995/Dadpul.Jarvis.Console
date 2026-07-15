// Made by Dadpul

namespace Dadpul.Jarvis.Console.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaToolCall
{
   #region Public Properties

   [JsonPropertyName("function")] public required OllamaCalledFunction Function { get; init; }

   [JsonPropertyName("type")] public string Type { get; init; } = "function";

   #endregion
}