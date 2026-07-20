// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Core.Conversation;
using System.ComponentModel.Composition;


public sealed class EchoChatModel : IChatModel
{
    public ChatModelDescriptor Descriptor => new ChatModelDescriptor("echo", ChatModelCapabilities.ConversationOnly, true);



    public int Priority => 0;
    #region IChatModel Members

    public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages,
      IReadOnlyList<ChatToolDefinition> tools, CancellationToken cancellationToken)
   {
      //var lastUserMessage = messages.LastOrDefault(message => message.Role == ChatRole.User);

      //var content = lastUserMessage is null ? "I have nothing to respond to." : $"You said: {lastUserMessage.Content}";

      var response = new ChatResponseChunk { Content = "If you see this, then there are absolutely no models loaded." +
          "\r\neither you are trying to run this on a potato pc or you've done fucked up boyo", Done = false, };

      var responseEnd = new ChatResponseChunk { Done = true, };
      yield return response;
      yield return responseEnd;
   }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    #endregion
}