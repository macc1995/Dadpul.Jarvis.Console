// Made by Dadpul

internal sealed class ChatResponse
{
   #region Public Properties

   public required string Content { get; init; }

   public ChatMetrics? Metrics { get; init; }

   #endregion
}