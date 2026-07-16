// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaChatResponse
{
   #region Public Properties

   [JsonPropertyName("done")] public bool Done { get; init; }

   [JsonPropertyName("done_reason")] public string? DoneReason { get; init; }

   [JsonPropertyName("eval_count")] public int EvaluationCount { get; init; }

   [JsonPropertyName("eval_duration")] public long EvaluationDuration { get; init; }

   [JsonPropertyName("load_duration")] public long LoadDuration { get; init; }

   [JsonPropertyName("message")] public OllamaChatMessage? Message { get; init; }

   [JsonPropertyName("model")] public required string Model { get; init; }

   [JsonPropertyName("prompt_eval_count")]
   public int PromptEvaluationCount { get; init; }

   [JsonPropertyName("prompt_eval_duration")]
   public long PromptEvaluationDuration { get; init; }

   [JsonPropertyName("total_duration")] public long TotalDuration { get; init; }

   #endregion
}