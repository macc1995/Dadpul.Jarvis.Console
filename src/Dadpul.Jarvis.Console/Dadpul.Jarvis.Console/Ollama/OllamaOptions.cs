// Bonjour

namespace Dadpul.Jarvis.Console.Ollama;

internal sealed class OllamaOptions
{
   #region Public Properties

   public required Uri BaseAddress { get; init; }

   public required string Model { get; init; }

   #endregion
}