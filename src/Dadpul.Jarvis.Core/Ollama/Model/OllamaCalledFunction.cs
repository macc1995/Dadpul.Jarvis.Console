// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama.Model;

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

internal sealed class OllamaCalledFunction
{
   #region Public Properties

   [JsonPropertyName("arguments")] public JsonObject Arguments { get; init; } = new();

   [JsonPropertyName("index")]
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public int? Index { get; init; }

   [JsonPropertyName("name")] public required string Name { get; init; }

   #endregion
}