// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama.Model;

using System.Text.Json.Serialization;

internal sealed class OllamaChatMessage
{
   #region Public Properties

   [JsonPropertyName("content")] public string Content { get; init; } = string.Empty;

   [JsonPropertyName("role")] public required string Role { get; init; }

   [JsonPropertyName("tool_calls")]
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public IReadOnlyList<OllamaToolCall>? ToolCalls { get; init; }

   [JsonPropertyName("tool_name")]
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public string? ToolName { get; init; }

   #endregion
}