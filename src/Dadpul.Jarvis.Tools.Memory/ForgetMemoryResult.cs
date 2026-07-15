// Made by Dadpul

using Dadpul.Jarvis.Tools.Memory;

public sealed class ForgetMemoryResult
{
   #region Public Properties

   public IReadOnlyList<MemoryMatch> Candidates { get; init; } = [];

   public MemoryRecord? DeletedMemory { get; init; }

   public required ForgetMemoryStatus Status { get; init; }

   #endregion

   #region Public Methods and Operators

   public static ForgetMemoryResult Ambiguous(IReadOnlyList<MemoryMatch> candidates)
   {
      return new ForgetMemoryResult { Status = ForgetMemoryStatus.Ambiguous, Candidates = candidates };
   }

   public static ForgetMemoryResult Deleted(MemoryRecord memory)
   {
      return new ForgetMemoryResult { Status = ForgetMemoryStatus.Deleted, DeletedMemory = memory };
   }

   public static ForgetMemoryResult NotFound()
   {
      return new ForgetMemoryResult { Status = ForgetMemoryStatus.NotFound };
   }

   #endregion
}