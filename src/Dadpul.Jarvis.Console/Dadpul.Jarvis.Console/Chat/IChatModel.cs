// Bonjour

namespace Dadpul.Jarvis.Console.Chat;

using Dadpul.Jarvis.Console.Conversation;

internal interface IChatModel
{
   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ChatToolDefinition> tools,
      CancellationToken cancellationToken);

   #endregion
}