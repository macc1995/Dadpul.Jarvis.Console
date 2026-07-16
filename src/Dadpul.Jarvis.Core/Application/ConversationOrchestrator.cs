// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application;

using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Core.Chat;
using Dadpul.Jarvis.Core.Conversation;
using Dadpul.Jarvis.Core.Tools;
using Dadpul.Jarvis.Interfaces.Tools;
using Dadpul.Jarvis.Interfaces.Tools.Memory;

using Console = System.Console;

internal sealed record ToolExecutionRecord(string ToolName, JsonObject Arguments, bool Success, string Result);

internal sealed record ResponseVerificationResult(bool Valid, string? Reason);

[Export(typeof(IConversationOrchestrator))]
public sealed class ConversationOrchestrator : IConversationOrchestrator
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions verifierJsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private readonly IChatModel chatModel;

   private readonly IMemoryRetriever memoryRetriever;

   private readonly IToolRegistry toolRegistry;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public ConversationOrchestrator(IChatModel chatModel, IMemoryRetriever memoryRetriever, IToolRegistry toolRegistry)
   {
      this.chatModel = chatModel;
      this.memoryRetriever = memoryRetriever;
      this.toolRegistry = toolRegistry;
   }

   #endregion

   #region IConversationOrchestrator Members

   public async IAsyncEnumerable<ChatResponseChunk> RespondAsync(ChatConversation conversation,
      [EnumeratorCancellation] CancellationToken cancellationToken)
   {
      IReadOnlyList<ChatToolDefinition> toolDefinitions = toolRegistry.Tools.Select(ConvertTool).ToList();

      const int maximumIterations = 10;

      var latestUserMessage = conversation.Messages.LastOrDefault(message => message.Role == ChatRole.User);

      var relevantMemories = latestUserMessage is null ? [] : await memoryRetriever.RetrieveAsync(latestUserMessage.Content, cancellationToken);

      /*
       * These records cover the complete current user turn, including
       * multiple model and tool-call iterations.
       */
      var executions = new List<ToolExecutionRecord>();

      /*
       * Rejected drafts are not added permanently to the conversation.
       * Instead, temporary correction instructions are added to subsequent
       * requests during this turn.
       */
      var correctionMessages = new List<ChatMessage>();

      for (var iteration = 0; iteration < maximumIterations; iteration++)
      {
         var assistantContent = new StringBuilder();
         var toolCalls = new List<ChatToolCall>();

         /*
          * We cannot stream assistant text immediately because it might contain
          * an unsupported claim such as "the container was stopped" without a
          * successful tool execution.
          */
         var bufferedVisibleChunks = new List<ChatResponseChunk>();

         var requestMessages = BuildRequestMessages(conversation, relevantMemories, correctionMessages);

         await foreach (var chunk in chatModel.GenerateResponseAsync(requestMessages, toolDefinitions, cancellationToken))
         {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
               assistantContent.Append(chunk.Content);
            }

            if (chunk.ToolCalls.Count > 0)
            {
               toolCalls.AddRange(chunk.ToolCalls);
            }

            /*
             * Preserve the existing visible behavior:
             * content and metrics are shown, but tool-call-only chunks are not.
             */
            if (!string.IsNullOrEmpty(chunk.Content) || chunk.Metrics is not null)
            {
               bufferedVisibleChunks.Add(chunk);
            }
         }

         if (toolCalls.Count == 0)
         {
            var draft = assistantContent.ToString();

            var verification = await VerifyResponseAsync(latestUserMessage?.Content ?? string.Empty, draft, executions, cancellationToken);

            if (!verification.Valid)
            {
               Console.WriteLine("verification invalid");
               var message = $"""
                              Your previous proposed response was rejected because it was
                              not grounded in the actual tool executions from this turn.

                              Rejected response:
                              <rejected-response>
                              {draft}
                              </rejected-response>

                              Verification failure:
                              {verification.Reason}

                              Continue fulfilling the user's original request.

                              Do not repeat an unsupported claim.
                              If an operation is still required, actually call the needed
                              tool.
                              If a tool failed, accurately report the failure.
                              If the request is already complete, provide a corrected
                              response supported by the tool results.
                              """;

               Console.WriteLine(message);
               correctionMessages.Add(new ChatMessage(ChatRole.System, message));

               continue;
            }

            /*
             * The verifier accepted the draft, so the buffered content can now
             * safely become visible.
             */
            foreach (var chunk in bufferedVisibleChunks)
            {
               yield return chunk;
            }

            yield break;
         }

         /*
          * This iteration contains actual tool calls. Release its introductory
          * text before running them.
          */
         foreach (var chunk in bufferedVisibleChunks)
         {
            yield return chunk;
         }

         conversation.AddAssistantToolCallMessage(assistantContent.ToString(), toolCalls);

         foreach (var toolCall in toolCalls)
         {
            var result = await ExecuteToolAsync(toolCall, cancellationToken);

            executions.Add(new ToolExecutionRecord(ToolName: toolCall.Name, Arguments: toolCall.Arguments.DeepClone().AsObject(),
               Success: result.Success, Result: result.Content));

            conversation.AddToolResultMessage(toolCall.Name, result.Content);
         }
      }

      throw new InvalidOperationException($"The model exceeded the maximum of " + $"{maximumIterations} response iterations.");
   }

   #endregion

   #region Methods

   private static IReadOnlyList<ChatMessage> BuildRequestMessages(ChatConversation conversation, IReadOnlyList<MemoryMatch> relevantMemories,
      IReadOnlyList<ChatMessage> correctionMessages)
   {
      var requestMessages = conversation.Messages.ToList();

      if (relevantMemories.Count > 0)
      {
         var memoryContent = string.Join(Environment.NewLine, relevantMemories.Select(match => $"""
                                                                                                <memory>
                                                                                                  <content>{match.Memory.Content}</content>
                                                                                                </memory>
                                                                                                """));

         var memoryContext = new ChatMessage(ChatRole.System, $"""
                                                               Potentially relevant long-term memories:

                                                               {memoryContent}

                                                               Use these memories only when relevant to the user's current request.
                                                               Do not mention that memories were retrieved unless asked.
                                                               Do not connect unrelated memories to the current topic.
                                                               """);

         var latestUserMessageIndex = requestMessages.FindLastIndex(message => message.Role == ChatRole.User);

         if (latestUserMessageIndex < 0)
         {
            requestMessages.Add(memoryContext);
         }
         else
         {
            requestMessages.Insert(latestUserMessageIndex, memoryContext);
         }
      }

      /*
       * Corrections are added last so they are the newest instructions seen by
       * the model. They remain temporary and are not stored in ChatConversation.
       */
      requestMessages.AddRange(correctionMessages);

      return requestMessages;
   }

   private static ChatToolDefinition ConvertTool(ITool tool)
   {
      return new ChatToolDefinition { Name = tool.Name, Description = tool.Description, Parameters = tool.Parameters };
   }

   private static string? ExtractJsonObject(string content)
   {
      if (string.IsNullOrWhiteSpace(content))
      {
         return null;
      }

      var trimmed = content.Trim();

      /*
       * This also handles a model incorrectly wrapping its JSON in a markdown
       * code block or adding a small amount of prose around it.
       */
      var objectStart = trimmed.IndexOf('{');
      var objectEnd = trimmed.LastIndexOf('}');

      if ((objectStart < 0) || (objectEnd <= objectStart))
      {
         return null;
      }

      return trimmed[objectStart..(objectEnd + 1)];
   }

   private static ResponseVerificationResult ParseVerificationResult(string verifierResponse)
   {
      var json = ExtractJsonObject(verifierResponse);

      if (string.IsNullOrWhiteSpace(json))
      {
         return new ResponseVerificationResult(Valid: false, Reason: "The response verifier returned no valid JSON object.");
      }

      try
      {
         var result = JsonSerializer.Deserialize<ResponseVerificationResult>(json, verifierJsonOptions);

         if (result is null)
         {
            return new ResponseVerificationResult(Valid: false, Reason: "The response verifier returned an empty verdict.");
         }

         if (!result.Valid && string.IsNullOrWhiteSpace(result.Reason))
         {
            return result with { Reason = "The response verifier rejected the draft without providing " + "a reason." };
         }

         return result;
      }
      catch (JsonException exception)
      {
         return new ResponseVerificationResult(Valid: false, Reason: $"The response verifier returned invalid JSON: " + $"{exception.Message}");
      }
   }

   private async Task<ToolResult> ExecuteToolAsync(ChatToolCall toolCall, CancellationToken cancellationToken)
   {
      Console.WriteLine();
      Console.WriteLine($"[Executing tool: {toolCall.Name}]");

      Console.WriteLine($"[Arguments: {toolCall.Arguments.ToJsonString()}]");

      if (!toolRegistry.TryGet(toolCall.Name, out var tool) || tool is null)
      {
         return ToolResult.Failed($"The requested tool '{toolCall.Name}' does not exist.");
      }

      try
      {
         return await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch (Exception exception)
      {
         return ToolResult.Failed($"Tool execution failed: {exception.Message}");
      }
   }

   private async Task<ResponseVerificationResult> VerifyResponseAsync(string userMessage, string assistantDraft,
      IReadOnlyList<ToolExecutionRecord> executions, CancellationToken cancellationToken)
   {
      var verificationPayload = JsonSerializer.Serialize(
         new
         {
            userRequest = userMessage,
            proposedResponse = assistantDraft,
            toolExecutions = executions.Select(execution => new
            {
               toolName = execution.ToolName, arguments = execution.Arguments, success = execution.Success, result = execution.Result
            })
         }, verifierJsonOptions);

      IReadOnlyList<ChatMessage> verificationMessages =
      [
         new ChatMessage(ChatRole.System, """
                                          You are a strict tool-execution grounding verifier.

                                          You do not fulfill the user's request.
                                          You only verify whether the proposed assistant response is supported
                                          by the actual tool executions supplied in the payload.

                                          A response is valid only when all claims about:
                                          - actions performed,
                                          - operations completed,
                                          - information retrieved,
                                          - external system state,
                                          - tool failures or successes

                                          are supported by the recorded tool calls and their results.

                                          Verification rules:

                                          1. Saying that an action will be performed does not mean it was
                                             performed.

                                          2. If the user requested an action, a response that merely promises
                                             or announces that action without completing it is invalid.

                                          3. A claim that an action succeeded requires a matching successful
                                             tool execution whose result supports that exact claim.

                                          4. A failed tool execution cannot support a claim of success.

                                          5. Do not infer success from the tool name alone. Read its result.

                                          6. If the response claims several targets were changed, the execution
                                             record must support every claimed target.

                                          7. If a tool result contradicts the response, the response is invalid.

                                          8. A factual answer about external state must be supported by a
                                             relevant successful tool result.

                                          9. Do not decide which specific tool should have been used.
                                             Only judge whether the response is grounded and whether the user's
                                             request was actually fulfilled.

                                          Return exactly one JSON object and no markdown:

                                          {
                                            "valid": true,
                                            "reason": null
                                          }

                                          or:

                                          {
                                            "valid": false,
                                            "reason": "A concise explanation of what is unsupported or missing."
                                          }
                                          """),

         new ChatMessage(ChatRole.User, $"""
                                         Verify this response using the following JSON payload:

                                         {verificationPayload}
                                         """)
      ];

      var verifierContent = new StringBuilder();
      var verifierProducedToolCall = false;

      await foreach (var chunk in chatModel.GenerateResponseAsync(verificationMessages, Array.Empty<ChatToolDefinition>(), cancellationToken))
      {
         if (!string.IsNullOrEmpty(chunk.Content))
         {
            verifierContent.Append(chunk.Content);
         }

         if (chunk.ToolCalls.Count > 0)
         {
            verifierProducedToolCall = true;
         }
      }

      if (verifierProducedToolCall)
      {
         return new ResponseVerificationResult(Valid: false,
            Reason: "The response verifier attempted to call a tool instead of " + "returning its JSON verdict.");
      }

      return ParseVerificationResult(verifierContent.ToString());
   }

   #endregion
}