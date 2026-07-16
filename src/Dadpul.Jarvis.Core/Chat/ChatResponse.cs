// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using Dadpul.Jarvis.Interfaces.Frontend;

internal sealed class ChatResponse
{
   #region Public Properties

   public required string Content { get; init; }

   public ChatMetrics? Metrics { get; init; }

   #endregion
}