namespace Dadpul.Jarvis.Tools.Memory
{
   public sealed class MemoryRecord
   {
      public required Guid Id { get; init; }

      public required string Content { get; init; }

      public required DateTimeOffset CreatedAt { get; init; }

      public DateTimeOffset? UpdatedAt { get; init; }
   }
}
