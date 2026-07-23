// Made by Dadpul

namespace Dadpul.Jarvis.Core.Conversation;

using Dadpul.Jarvis.Core.Application;

public interface IConversationProvider
{
   #region Public Methods and Operators

   ConversationSession GetConversation(string frontendName, string conversationId);

   #endregion
}