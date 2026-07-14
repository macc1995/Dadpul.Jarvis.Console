// Bonjour

namespace Dadpul.Jarvis.Console.Application;

using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Text;

using Dadpul.Jarvis.Console.Chat;
using Dadpul.Jarvis.Console.Conversation;
using Dadpul.Jarvis.Console.Tools;
using Dadpul.Jarvis.Interfaces.Tools;
using Dadpul.Jarvis.Tools.Memory;

internal interface IConversationOrchestrator
{
   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> RespondAsync(ChatConversation conversation, [EnumeratorCancellation] CancellationToken cancellationToken);

   #endregion
}

[Export(typeof(IConversationOrchestrator))]
internal sealed class ConversationOrchestrator : IConversationOrchestrator
{
   #region Constants and Fields

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

      for (var iteration = 0; iteration < maximumIterations; iteration++)
      {
         var assistantContent = new StringBuilder();
         var toolCalls = new List<ChatToolCall>();
         var requestMessages = BuildRequestMessages(conversation, relevantMemories);
         await foreach (var chunk in chatModel.GenerateResponseAsync(requestMessages, toolDefinitions, cancellationToken))
         {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
               assistantContent.Append(chunk.Content);
               yield return chunk;
            }

            if (chunk.ToolCalls.Count > 0)
            {
               toolCalls.AddRange(chunk.ToolCalls);
            }

            if (chunk.Metrics is not null)
            {
               yield return chunk;
            }
         }

         if (toolCalls.Count == 0)
         {
            yield break;
         }

         conversation.AddAssistantToolCallMessage(assistantContent.ToString(), toolCalls);

         foreach (var toolCall in toolCalls)
         {
            var result = await ExecuteToolAsync(toolCall, cancellationToken);

            conversation.AddToolResultMessage(toolCall.Name, result.Content);
         }
      }

      throw new InvalidOperationException($"The model exceeded the maximum of " + $"{maximumIterations} tool-call iterations.");
   }

   #endregion

   #region Methods

   private static IReadOnlyList<ChatMessage> BuildRequestMessages(ChatConversation conversation, IReadOnlyList<MemoryMatch> relevantMemories)
   {
      if (relevantMemories.Count == 0)
      {
         return conversation.Messages;
      }

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

      var requestMessages = conversation.Messages.ToList();

      var latestUserMessageIndex = requestMessages.FindLastIndex(message => message.Role == ChatRole.User);

      if (latestUserMessageIndex < 0)
      {
         requestMessages.Add(memoryContext);
      }
      else
      {
         requestMessages.Insert(latestUserMessageIndex, memoryContext);
      }

      return requestMessages;
   }

   private static ChatToolDefinition ConvertTool(ITool tool)
   {
      return new ChatToolDefinition { Name = tool.Name, Description = tool.Description, Parameters = tool.Parameters };
   }

   private async Task<ToolResult> ExecuteToolAsync(ChatToolCall toolCall, CancellationToken cancellationToken)
   {
      System.Console.WriteLine();
      System.Console.WriteLine($"[Executing tool: {toolCall.Name}]");

      System.Console.WriteLine($"[Arguments: {toolCall.Arguments.ToJsonString()}]");

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

   #endregion
}