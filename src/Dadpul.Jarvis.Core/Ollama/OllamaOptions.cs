// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama;

public sealed class OllamaOptions
{
   #region Public Properties

   public required Uri BaseAddress { get; init; }

   public required string EmbeddingModel { get; init; }

   public required string Model { get; init; }

   public bool Think { get; init; } = false;

   #endregion
}