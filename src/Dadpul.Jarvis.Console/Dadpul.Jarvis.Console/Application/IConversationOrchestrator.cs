// Made by Dadpul

namespace Dadpul.Jarvis.Console.Application;

using System.Runtime.CompilerServices;

using Dadpul.Jarvis.Console.Chat;
using Dadpul.Jarvis.Console.Conversation;

internal interface IConversationOrchestrator
{
   #region Public Methods and Operators

   IAsyncEnumerable<ChatResponseChunk> RespondAsync(ChatConversation conversation, [EnumeratorCancellation] CancellationToken cancellationToken);

   #endregion
}