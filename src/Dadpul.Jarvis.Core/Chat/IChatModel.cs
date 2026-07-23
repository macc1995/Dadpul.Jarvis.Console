// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Core.Application.Propmpts;
using Dadpul.Jarvis.Core.Conversation;

public interface IChatModel
{
   #region Public Properties

   ChatModelDescriptor Descriptor { get; }

   int Priority { get; }

   ISystemPrompt SystemPrompt { get; }

   #endregion

   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ChatToolDefinition> tools,
      CancellationToken cancellationToken);

   Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

   #endregion
}