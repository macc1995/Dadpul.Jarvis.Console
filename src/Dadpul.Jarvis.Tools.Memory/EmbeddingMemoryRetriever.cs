// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Memory;

using System.ComponentModel.Composition;

using Dadpul.Jarvis.Embeddings;

[Export(typeof(IMemoryRetriever))]
public sealed class EmbeddingMemoryRetriever : IMemoryRetriever
{
   #region Constants and Fields

   private readonly IEmbeddingGenerator embeddingGenerator;

   private readonly int maximumResults;

   private readonly IMemoryStore memoryStore;

   private readonly float minimumSimilarity;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public EmbeddingMemoryRetriever(IMemoryStore memoryStore, IEmbeddingGenerator embeddingGenerator)
   {
      this.memoryStore = memoryStore;
      this.embeddingGenerator = embeddingGenerator;
      maximumResults = 3;
      minimumSimilarity = 0.45f;
   }

   #endregion

   #region IMemoryRetriever Members

   public async Task<IReadOnlyList<MemoryMatch>> RetrieveAsync(string query, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(query))
      {
         return [];
      }

      var memories = await memoryStore.GetAllAsync(cancellationToken);

      if (memories.Count == 0)
      {
         return [];
      }

      var queryEmbedding = await embeddingGenerator.GenerateAsync(query.Trim(), EmbeddingInputType.Query, cancellationToken);

      var rankedMatches = memories.Where(memory => memory.Embedding.Length == queryEmbedding.Length)
         .Select(memory => new MemoryMatch { Memory = memory, Similarity = CalculateCosineSimilarity(queryEmbedding, memory.Embedding) })
         .OrderByDescending(match => match.Similarity).ToList();

      foreach (var match in rankedMatches)
      {
         Console.WriteLine($"[Memory candidate: {match.Similarity:F4}] " + $"{match.Memory.Id} | {match.Memory.Content}");
      }

      return rankedMatches.Where(match => match.Similarity >= minimumSimilarity).Take(maximumResults).ToList();
   }

   #endregion

   #region Methods

   private static float CalculateCosineSimilarity(ReadOnlySpan<float> first, ReadOnlySpan<float> second)
   {
      if (first.Length != second.Length)
      {
         throw new ArgumentException("Embedding dimensions must match.");
      }

      double dotProduct = 0;
      double firstMagnitude = 0;
      double secondMagnitude = 0;

      for (var index = 0; index < first.Length; index++)
      {
         dotProduct += first[index] * second[index];
         firstMagnitude += first[index] * first[index];
         secondMagnitude += second[index] * second[index];
      }

      if ((firstMagnitude == 0) || (secondMagnitude == 0))
      {
         return 0;
      }

      return (float)(dotProduct / (Math.Sqrt(firstMagnitude) * Math.Sqrt(secondMagnitude)));
   }

   #endregion
}