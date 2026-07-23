// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaRequestOptions
{
   #region Public Properties

   [JsonPropertyName("num_ctx")] public int ContextSize { get; init; }

   #endregion
}
