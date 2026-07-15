// Made by Dadpul

namespace Dadpul.Jarvis.Console.Ollama.Model;

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

internal sealed class OllamaToolFunction
{
   #region Public Properties

   [JsonPropertyName("description")] public required string Description { get; init; }

   [JsonPropertyName("name")] public required string Name { get; init; }

   [JsonPropertyName("parameters")] public required JsonObject Parameters { get; init; }

   #endregion
}