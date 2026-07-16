// Made by Dadpul

namespace Dadpul.Jarvis.Interfaces.Frontend;

public interface IFrontend
{
   #region Public Properties

   string Name { get; }

   #endregion

   #region Public Methods and Operators

   Task BeginResponseAsync(string conversationId, CancellationToken cancellationToken);

   Task CompleteResponseAsync(string conversationId, ChatMetrics? metrics, CancellationToken cancellationToken);

   Task RunAsync(Func<FrontendMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken);

   Task WriteResponseChunkAsync(string conversationId, string content, CancellationToken cancellationToken);

   #endregion
}