// Bonjour

namespace Dadpul.Jarvis.Console.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaChatRequest
{
   #region Public Properties

   [JsonPropertyName("messages")] public required IReadOnlyList<OllamaChatMessage> Messages { get; init; }

   [JsonPropertyName("model")] public required string Model { get; init; }

   [JsonPropertyName("stream")] public bool Stream { get; init; }

   [JsonPropertyName("tools")] public IReadOnlyList<OllamaToolDefinition>? Tools { get; init; }

   #endregion
}