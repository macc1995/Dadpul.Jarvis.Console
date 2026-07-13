// Bonjour

using Dadpul.Jarvis.Console.Chat;
using Dadpul.Jarvis.Console.Conversation;

internal sealed class EchoChatModel : IChatModel
{
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