using System.Text.Json.Serialization;
namespace Dadpul.Jarvis.Console.Ollama.Model;

internal sealed class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; init; }

    [JsonPropertyName("total_duration")]
    public long TotalDuration { get; init; }

    [JsonPropertyName("load_duration")]
    public long LoadDuration { get; init; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvaluationCount { get; init; }

    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvaluationDuration { get; init; }

    [JsonPropertyName("eval_count")]
    public int EvaluationCount { get; init; }

    [JsonPropertyName("eval_duration")]
    public long EvaluationDuration { get; init; }
}