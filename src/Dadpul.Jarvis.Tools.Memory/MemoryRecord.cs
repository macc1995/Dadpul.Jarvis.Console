namespace Dadpul.Jarvis.Tools.Memory
{
   public sealed class MemoryRecord
   {
      public required Guid Id { get; init; }

      public required string Content { get; init; }
      public required float[] Embedding { get; init; }

      public required string EmbeddingModel { get; init; }

      public required string EmbeddingVersion { get; init; }
      public required DateTimeOffset CreatedAt { get; init; }

      public DateTimeOffset? UpdatedAt { get; init; }
   }
}
