// Bonjour

namespace Dadpul.Jarvis.Console.Ollama;

using System.Composition;
using System.Net.Http.Json;

using Dadpul.Jarvis.Embeddings;

[Export(typeof(IEmbeddingGenerator))]
public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
   #region Constants and Fields

   private readonly HttpClient httpClient;

   private readonly OllamaOptions options;

   #endregion

   #region Constructors and Destructors

   public OllamaEmbeddingGenerator(HttpClient httpClient, OllamaOptions options)
   {
      this.httpClient = httpClient;
      this.options = options;
   }

   #endregion

   #region IEmbeddingGenerator Members

   public async Task<float[]> GenerateAsync(string input, EmbeddingInputType inputType, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(input))
      {
         throw new ArgumentException("Embedding input cannot be empty.", nameof(input));
      }

      var request = new OllamaEmbeddingRequest { Model = options.EmbeddingModel, Input = PrepareInput(input.Trim(), inputType), Truncate = false };

      using var response = await httpClient.PostAsJsonAsync("api/embed", request, cancellationToken);

      response.EnsureSuccessStatusCode();

      var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);

      if (result is null)
      {
         throw new InvalidOperationException("Ollama returned an empty embedding response.");
      }

      if (result.Embeddings.Count != 1)
      {
         throw new InvalidOperationException($"Expected one embedding but received " + $"{result.Embeddings.Count}.");
      }

      var embedding = result.Embeddings[0];

      if (embedding.Length == 0)
      {
         throw new InvalidOperationException("Ollama returned an empty embedding vector.");
      }

      return embedding;
   }

   #endregion

   #region Methods

   private static string PrepareInput(string input, EmbeddingInputType inputType)
   {
      return inputType switch
      {
         EmbeddingInputType.Query => $"task: search query | query: {input}",

         EmbeddingInputType.Document => $"task: search result | query: {input}",

         _ => throw new ArgumentOutOfRangeException(nameof(inputType), inputType, "Unsupported embedding input type.")
      };
   }

   #endregion
}