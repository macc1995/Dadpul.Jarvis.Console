// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Core.Application.Propmpts;
using Dadpul.Jarvis.Core.Conversation;

public interface IChatModel
{
    #region Public Methods and Operators

    int Priority { get; }
    ISystemPrompt SystemPrompt { get; }
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken);
    IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ChatToolDefinition> tools,
      CancellationToken cancellationToken);

    ChatModelDescriptor Descriptor { get; }
    #endregion
}

public sealed record ChatModelDescriptor(
   string Name,
   ChatModelCapabilities Capabilities,
   bool IsFallback);
public enum ChatModelCapabilities
{
    ConversationOnly = 0,
    BasicTools = 1,
    FullTools = 2
}
