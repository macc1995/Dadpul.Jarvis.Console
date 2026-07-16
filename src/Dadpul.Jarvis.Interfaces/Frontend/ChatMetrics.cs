// Made by Dadpul

namespace Dadpul.Jarvis.Interfaces.Frontend;

public sealed class ChatMetrics
{
   #region Public Properties

   public string? FinishReason { get; init; }

   public int GeneratedTokenCount { get; init; }

   public TimeSpan GenerationDuration { get; init; }

   public double GenerationTokensPerSecond => CalculateTokensPerSecond(GeneratedTokenCount, GenerationDuration);

   public TimeSpan LoadDuration { get; init; }

   public required string Model { get; init; }

   public TimeSpan PromptEvaluationDuration { get; init; }

   public int PromptTokenCount { get; init; }

   public double PromptTokensPerSecond => CalculateTokensPerSecond(PromptTokenCount, PromptEvaluationDuration);

   public TimeSpan TotalDuration { get; init; }

   #endregion

   #region Methods

   private static double CalculateTokensPerSecond(int tokenCount, TimeSpan duration)
   {
      if ((tokenCount == 0) || (duration <= TimeSpan.Zero))
      {
         return 0;
      }

      return tokenCount / duration.TotalSeconds;
   }

   #endregion
}