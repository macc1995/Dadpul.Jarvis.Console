// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama;

public sealed class OllamaOptions
{
   #region Public Properties

   public static string SectionName { get; } = "Ollama";

   public Uri? BaseAddress { get; set; }

   public int ContextSize { get; set; } = 8192;

   public string EmbeddingModel { get; set; } = string.Empty;

   public string Model { get; set; } = string.Empty;

   public OllamaPreloadOptions Preload { get; set; } = new();

   public bool Think { get; set; }

   #endregion
}
