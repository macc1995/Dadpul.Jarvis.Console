// Made by Dadpul

using System.Text.Json.Serialization;

internal sealed class OllamaEmbeddingRequest
{
   #region Public Properties

   [JsonPropertyName("input")] public required string Input { get; init; }

   [JsonPropertyName("model")] public required string Model { get; init; }

   [JsonPropertyName("truncate")] public bool Truncate { get; init; } = false;

   #endregion
}