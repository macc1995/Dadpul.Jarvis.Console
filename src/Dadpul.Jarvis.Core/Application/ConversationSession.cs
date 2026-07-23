// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application;

using Dadpul.Jarvis.Core.Conversation;

public sealed class ConversationSession
{
   #region Constructors and Destructors

   public ConversationSession(ChatConversation conversation)
   {
      ArgumentNullException.ThrowIfNull(conversation);

      Conversation = conversation;
   }

   #endregion

   #region Public Properties

   public ChatConversation Conversation { get; }

   public SemaphoreSlim Lock { get; } = new(1, 1);

   #endregion
}