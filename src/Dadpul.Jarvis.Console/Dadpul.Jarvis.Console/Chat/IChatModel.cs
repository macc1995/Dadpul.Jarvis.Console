using Dadpul.Jarvis.Console.Conversation;

namespace Dadpul.Jarvis.Console.Chat;

internal interface IChatModel
{
    IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        CancellationToken cancellationToken);
}
