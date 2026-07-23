// Made by Dadpul

namespace Dadpul.Jarvis.Core.Conversation;

using System.Collections.Concurrent;

using Dadpul.Jarvis.Core.Application;

public sealed class InMemoryConversationProvider : IConversationProvider
{
   #region Constants and Fields

   private readonly Func<ChatConversation> conversationFactory;

   private readonly ConcurrentDictionary<string, ConversationSession> conversations = [];

   #endregion

   #region Constructors and Destructors

   public InMemoryConversationProvider(Func<ChatConversation> conversationFactory)
   {
      ArgumentNullException.ThrowIfNull(conversationFactory);

      this.conversationFactory = conversationFactory;
   }

   #endregion

   #region IConversationProvider Members

   public ConversationSession GetConversation(string frontendName, string conversationId)
   {
      if (string.IsNullOrWhiteSpace(frontendName))
      {
         throw new ArgumentException("A frontend name is required.", nameof(frontendName));
      }

      if (string.IsNullOrWhiteSpace(conversationId))
      {
         throw new ArgumentException("A conversation ID is required.", nameof(conversationId));
      }

      var key = $"{frontendName}:{conversationId}";

      return conversations.GetOrAdd(key, _ => new ConversationSession(conversationFactory()));
   }

   #endregion
}