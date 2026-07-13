using Dadpul.Jarvis.Console.Chat;
using Dadpul.Jarvis.Console.Conversation;
using Dadpul.Jarvis.Console.Tools;
using System.Runtime.CompilerServices;
using System.Text;

namespace Dadpul.Jarvis.Console.Application;

internal sealed class ConversationOrchestrator
{
    private readonly IChatModel chatModel;
    private readonly ToolRegistry toolRegistry;

    public ConversationOrchestrator(
        IChatModel chatModel,
        ToolRegistry toolRegistry)
    {
        this.chatModel = chatModel;
        this.toolRegistry = toolRegistry;
    }

    public async IAsyncEnumerable<ChatResponseChunk> RespondAsync(
    ChatConversation conversation,
    [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatToolDefinition> toolDefinitions =
            toolRegistry.Tools
                .Select(ConvertTool)
                .ToList();

        const int maximumIterations = 10;

        for (int iteration = 0;
             iteration < maximumIterations;
             iteration++)
        {
            var assistantContent = new StringBuilder();
            var toolCalls = new List<ChatToolCall>();

            await foreach (ChatResponseChunk chunk
                           in chatModel.GenerateResponseAsync(
                               conversation.Messages,
                               toolDefinitions,
                               cancellationToken))
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

            conversation.AddAssistantToolCallMessage(
                assistantContent.ToString(),
                toolCalls);

            foreach (ChatToolCall toolCall in toolCalls)
            {
                ToolResult result = await ExecuteToolAsync(
                    toolCall,
                    cancellationToken);

                conversation.AddToolResultMessage(
                    toolCall.Name,
                    result.Content);
            }
        }

        throw new InvalidOperationException(
            $"The model exceeded the maximum of " +
            $"{maximumIterations} tool-call iterations.");
    }

    private async Task<ToolResult> ExecuteToolAsync(
    ChatToolCall toolCall,
    CancellationToken cancellationToken)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(
            $"[Executing tool: {toolCall.Name}]");

        if (!toolRegistry.TryGet(
                toolCall.Name,
                out ITool? tool) ||
            tool is null)
        {
            return ToolResult.Failed(
                $"The requested tool '{toolCall.Name}' does not exist.");
        }

        try
        {
            return await tool.ExecuteAsync(
                toolCall.Arguments,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ToolResult.Failed(
                $"Tool execution failed: {exception.Message}");
        }
    }

    private static ChatToolDefinition ConvertTool(ITool tool)
    {
        return new ChatToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = tool.Parameters
        };
    }
}