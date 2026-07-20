// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Core.Conversation;

internal sealed class EchoChatModel : IChatModel
{
    public ChatModelDescriptor Descriptor => new ChatModelDescriptor("echo", ChatModelCapabilities.ConversationOnly, true);
    #region IChatModel Members

    public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages,
      IReadOnlyList<ChatToolDefinition> tools, CancellationToken cancellationToken)
   {
      var lastUserMessage = messages.LastOrDefault(message => message.Role == ChatRole.User);

      var content = lastUserMessage is null ? "I have nothing to respond to." : $"You said: {lastUserMessage.Content}";

      var response = new ChatResponseChunk { Content = content, Done = false, };

      var responseEnd = new ChatResponseChunk { Done = true, };
      yield return response;
      yield return responseEnd;
   }

   #endregion
}