// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application;

using System.Runtime.CompilerServices;

using Dadpul.Jarvis.Core.Chat;
using Dadpul.Jarvis.Core.Conversation;

public interface IConversationOrchestrator
{
   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> RespondAsync(ChatConversation conversation, [EnumeratorCancellation] CancellationToken cancellationToken);

   #endregion
}