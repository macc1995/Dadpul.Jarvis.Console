// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Memory;

using System.ComponentModel.Composition;

using Dadpul.Jarvis.Embeddings;
using Dadpul.Jarvis.Interfaces.Tools.Memory;

[Export(typeof(IMemoryService))]
public sealed class MemoryService : IMemoryService
{
   #region Constants and Fields

   private const string EmbeddingVersion = "retrieval-v1";

   private const float MinimumDeleteSimilarity = 0.55f;

   private const float MinimumLead = 0.05f;

   private readonly IEmbeddingGenerator embeddingGenerator;

   private readonly string embeddingModel;

   private readonly IMemoryRetriever memoryRetriever;

   private readonly IMemoryStore memoryStore;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public MemoryService(IMemoryStore memoryStore, IMemoryRetriever memoryRetriever, IEmbeddingGenerator embeddingGenerator,
      [Import("embeddingModel")] string embeddingModel)
   {
      this.memoryStore = memoryStore;
      this.memoryRetriever = memoryRetriever;
      this.embeddingGenerator = embeddingGenerator;
      this.embeddingModel = embeddingModel;
   }

   #endregion

   #region Public Methods and Operators

   public async Task<ForgetMemoryResult> ForgetAsync(string query, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(query))
      {
         throw new ArgumentException("Memory query cannot be empty.", nameof(query));
      }

      var matches = await memoryRetriever.RetrieveAsync(query.Trim(), cancellationToken);

      if (matches.Count == 0)
      {
         return ForgetMemoryResult.NotFound();
      }

      var bestMatch = matches[0];

      if (bestMatch.Similarity < MinimumDeleteSimilarity)
      {
         return ForgetMemoryResult.NotFound();
      }

      if (matches.Count > 1)
      {
         var secondBestMatch = matches[1];

         var lead = bestMatch.Similarity - secondBestMatch.Similarity;

         if (lead < MinimumLead)
         {
            return ForgetMemoryResult.Ambiguous(matches);
         }
      }

      var deleted = await memoryStore.DeleteAsync(bestMatch.Memory.Id, cancellationToken);

      if (!deleted)
      {
         return ForgetMemoryResult.NotFound();
      }

      return ForgetMemoryResult.Deleted(bestMatch.Memory);
   }

   public async Task<MemoryRecord> RememberAsync(string content, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(content))
      {
         throw new ArgumentException("Memory content cannot be empty.", nameof(content));
      }

      var normalizedContent = content.Trim();

      var embedding = await embeddingGenerator.GenerateAsync(normalizedContent, EmbeddingInputType.Document, cancellationToken);

      var memory = new MemoryRecord
      {
         Id = Guid.NewGuid(),
         Content = normalizedContent,
         Embedding = embedding,
         EmbeddingModel = embeddingModel,
         EmbeddingVersion = EmbeddingVersion,
         CreatedAt = DateTimeOffset.UtcNow
      };

      await memoryStore.StoreAsync(memory, cancellationToken);

      return memory;
   }

   #endregion
}