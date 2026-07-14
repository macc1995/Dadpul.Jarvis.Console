// Bonjour

namespace Dadpul.Jarvis.Console.Ollama;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Dadpul.Jarvis.Console.Chat;
using Dadpul.Jarvis.Console.Conversation;
using Dadpul.Jarvis.Console.Ollama.Model;

internal sealed class OllamaChatModel : IChatModel
{
   #region Constants and Fields

   private readonly HttpClient httpClient;

   private readonly OllamaOptions options;

   #endregion

   #region Constructors and Destructors

   public OllamaChatModel(HttpClient httpClient, OllamaOptions options)
   {
      this.httpClient = httpClient;
      this.options = options;
   }

   #endregion

   #region IChatModel Members

   public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages,
      IReadOnlyList<ChatToolDefinition> tools, [EnumeratorCancellation] CancellationToken cancellationToken)
   {
      var requestBody = new OllamaChatRequest
      {
         Model = options.Model,
         Messages = messages.Select(ConvertMessage).ToList(),
         Tools = tools.Select(ConvertTool).ToList(),
         Stream = true,
         Think = options.Think
      };

      using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat") { Content = JsonContent.Create(requestBody) };

      using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

      response.EnsureSuccessStatusCode();

      await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

      using var reader = new StreamReader(responseStream, Encoding.UTF8);

      while (!reader.EndOfStream)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var line = await reader.ReadLineAsync(cancellationToken);

         if (string.IsNullOrWhiteSpace(line))
         {
            continue;
         }

         var result = JsonSerializer.Deserialize<OllamaChatResponse>(line);

         if (result is null)
         {
            continue;
         }

         if (!string.IsNullOrEmpty(result.Message?.Content))
         {
            yield return new ChatResponseChunk { Content = result.Message.Content };
         }

         if (result.Message?.ToolCalls is { Count: > 0 })
         {
            yield return new ChatResponseChunk { ToolCalls = result.Message.ToolCalls.Select(ConvertToolCall).ToList() };
         }

         if (result.Done)
         {
            yield return new ChatResponseChunk { Done = true, Metrics = ConvertMetrics(result) };

            yield break;
         }
      }
   }

   #endregion

   #region Methods

   private static OllamaChatMessage ConvertMessage(ChatMessage message)
   {
      return new OllamaChatMessage
      {
         Role = ConvertRole(message.Role),
         Content = message.Content,
         ToolName = message.ToolName,
         ToolCalls = message.ToolCalls?.Select((toolCall, index) => ConvertToolCall(toolCall, index)).ToList()
      };
   }

   private static ChatMetrics ConvertMetrics(OllamaChatResponse result)
   {
      return new ChatMetrics
      {
         Model = result.Model,
         FinishReason = result.DoneReason,
         PromptTokenCount = result.PromptEvaluationCount,
         GeneratedTokenCount = result.EvaluationCount,
         LoadDuration = ConvertNanoseconds(result.LoadDuration),
         PromptEvaluationDuration = ConvertNanoseconds(result.PromptEvaluationDuration),
         GenerationDuration = ConvertNanoseconds(result.EvaluationDuration),
         TotalDuration = ConvertNanoseconds(result.TotalDuration)
      };
   }

   private static TimeSpan ConvertNanoseconds(long nanoseconds)
   {
      return TimeSpan.FromTicks(nanoseconds / 100);
   }

   private static string ConvertRole(ChatRole role)
   {
      return role switch
      {
         ChatRole.System => "system",
         ChatRole.User => "user",
         ChatRole.Assistant => "assistant",
         ChatRole.Tool => "tool",

         _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported chat role.")
      };
   }

   private static OllamaToolDefinition ConvertTool(ChatToolDefinition tool)
   {
      return new OllamaToolDefinition
      {
         Function = new OllamaToolFunction { Name = tool.Name, Description = tool.Description, Parameters = tool.Parameters }
      };
   }

   private static ChatToolCall ConvertToolCall(OllamaToolCall toolCall)
   {
      return new ChatToolCall { Name = toolCall.Function.Name, Arguments = toolCall.Function.Arguments };
   }

   private static OllamaToolCall ConvertToolCall(ChatToolCall toolCall, int index)
   {
      return new OllamaToolCall { Function = new OllamaCalledFunction { Index = index, Name = toolCall.Name, Arguments = toolCall.Arguments } };
   }

   #endregion
}