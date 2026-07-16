// Made by Dadpul

namespace Dadpul.Jarvis.Core.Conversation;

using System.Collections.Concurrent;

using Dadpul.Jarvis.Core.Application;
using Dadpul.Jarvis.Core.Chat;

public sealed class ChatConversation
{
   #region Constants and Fields

   private readonly List<ChatMessage> messages = [];

   #endregion

   #region Public Properties

   public IReadOnlyList<ChatMessage> Messages => messages;

   #endregion

   #region Public Methods and Operators

   public void AddAssistantMessage(string content)
   {
      AddMessage(ChatRole.Assistant, content);
   }

   public void AddAssistantToolCallMessage(string content, IReadOnlyList<ChatToolCall> toolCalls)
   {
      ArgumentNullException.ThrowIfNull(toolCalls);

      if (toolCalls.Count == 0)
      {
         throw new ArgumentException("At least one tool call is required.", nameof(toolCalls));
      }

      messages.Add(new ChatMessage(ChatRole.Assistant, content, toolCalls));
   }

   public void AddSystemMessage(string content)
   {
      AddMessage(ChatRole.System, content);
   }

   public void AddToolResultMessage(string toolName, string content)
   {
      if (string.IsNullOrWhiteSpace(toolName))
      {
         throw new ArgumentException("A tool name is required.", nameof(toolName));
      }

      if (string.IsNullOrWhiteSpace(content))
      {
         throw new ArgumentException("A tool result cannot be empty.", nameof(content));
      }

      messages.Add(new ChatMessage(ChatRole.Tool, content.Trim(), ToolName: toolName));
   }

   public void AddUserMessage(string content)
   {
      AddMessage(ChatRole.User, content);
   }

   #endregion

   #region Methods

   private void AddMessage(ChatRole role, string content)
   {
      if (string.IsNullOrWhiteSpace(content))
      {
         throw new ArgumentException("A chat message cannot be empty.", nameof(content));
      }

      messages.Add(new ChatMessage(role, content.Trim()));
   }

   #endregion
}

public interface IConversationProvider
{
   #region Public Methods and Operators

   ConversationSession GetConversation(string frontendName, string conversationId);

   #endregion
}

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