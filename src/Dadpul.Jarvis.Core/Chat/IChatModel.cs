// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Core.Conversation;

public interface IChatModel
{
   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ChatToolDefinition> tools,
      CancellationToken cancellationToken);

   #endregion
}