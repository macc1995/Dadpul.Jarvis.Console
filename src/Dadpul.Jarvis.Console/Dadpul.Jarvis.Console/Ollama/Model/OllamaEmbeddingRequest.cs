// Bonjour

using System.Text.Json.Serialization;

internal sealed class OllamaEmbeddingRequest
{
   #region Public Properties

   [JsonPropertyName("input")] public required string Input { get; init; }

   [JsonPropertyName("model")] public required string Model { get; init; }

   [JsonPropertyName("truncate")] public bool Truncate { get; init; } = false;

   #endregion
}

internal sealed class OllamaEmbeddingResponse
{
   #region Public Properties

   [JsonPropertyName("embeddings")] public required IReadOnlyList<float[]> Embeddings { get; init; }

   [JsonPropertyName("load_duration")] public long LoadDuration { get; init; }

   [JsonPropertyName("model")] public required string Model { get; init; }

   [JsonPropertyName("prompt_eval_count")]
   public int PromptEvaluationCount { get; init; }

   [JsonPropertyName("total_duration")] public long TotalDuration { get; init; }

   #endregion
}