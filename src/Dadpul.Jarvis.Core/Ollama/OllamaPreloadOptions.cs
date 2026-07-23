// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama;

public sealed class OllamaPreloadOptions
{
   #region Public Properties

   public bool Enabled { get; set; } = true;

   public int KeepAlive { get; set; } = -1;

   #endregion
}