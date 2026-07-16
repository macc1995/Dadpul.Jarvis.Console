// Made by Dadpul

namespace Dadpul.Jarvis.Interfaces.Tools.Memory;

public sealed class MemoryMatch
{
   #region Public Properties

   public required MemoryRecord Memory { get; init; }

   public required float Similarity { get; init; }

   #endregion
}