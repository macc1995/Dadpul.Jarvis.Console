// Made by Dadpul

namespace Dadpul.Jarvis.Console
{
   using System.ComponentModel.Composition;
   using System.ComponentModel.Composition.Hosting;
   using System.Net.Http.Json;
   using System.Reflection;

   using Dadpul.Jarvis.Console.Application;
   using Dadpul.Jarvis.Console.Application.Propmpts;
   using Dadpul.Jarvis.Console.Chat;
   using Dadpul.Jarvis.Console.Conversation;
   using Dadpul.Jarvis.Console.Ollama;
   using Dadpul.Jarvis.Console.Tools;
   using Dadpul.Jarvis.Embeddings;
   using Dadpul.Jarvis.Interfaces.Tools;

   using Console = System.Console;

   internal class Program
   {
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

      static async Task Main(string[] args)
      {
         Console.InputEncoding = System.Text.Encoding.UTF8;
         Console.OutputEncoding = System.Text.Encoding.UTF8;

         var conversation = new ChatConversation();
         conversation.AddSystemMessage(JarvisSystemPrompt.Content);
         var ollamaOptions = new OllamaOptions
         {
            BaseAddress = new Uri("http://192.168.0.69:11434"),
            Model = "qwen3:4b-instruct-2507-q4_K_M",
            Think = false,
            EmbeddingModel = "embeddinggemma"
         };

         using var httpClient = new HttpClient { BaseAddress = ollamaOptions.BaseAddress };

         IChatModel chatModel = new OllamaChatModel(httpClient, ollamaOptions);
         IEmbeddingGenerator embeddingGenerator = new OllamaEmbeddingGenerator(httpClient, ollamaOptions);
         var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                 ?? throw new InvalidOperationException("Unable to resolve application directory.");

         using var container = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()),
            new DirectoryCatalog(assemblyDirectory, "Dadpul.Jarvis.Tools.*.dll"));
         using var catalog = new CompositionContainer(container);
         var compositionBatch = new CompositionBatch();
         compositionBatch.AddExportedValue(chatModel);
         compositionBatch.AddExportedValue(embeddingGenerator);
         compositionBatch.AddExportedValue("embeddingModel", ollamaOptions.EmbeddingModel);
         catalog.Compose(compositionBatch);

         var registry = catalog.GetExportedValue<IToolRegistry>();

         foreach (var tool in catalog.GetExportedValues<ITool>())
         {
            Console.WriteLine($"registering {tool.Name} {tool.Version}");
            registry.Register(tool);
         }

         //var memoryEmbedding =
         //   await embeddingGenerator.GenerateAsync("The user prefers Celsius.", EmbeddingInputType.Document, CancellationToken.None);

         //var relatedQuestionEmbedding =
         //   await embeddingGenerator.GenerateAsync("Which temperature unit should I use?", EmbeddingInputType.Query, CancellationToken.None);

         //var unrelatedQuestionEmbedding =
         //   await embeddingGenerator.GenerateAsync("What coffee machine does the user own?", EmbeddingInputType.Query, CancellationToken.None);
         //var relatedSimilarity = CalculateCosineSimilarity(memoryEmbedding, relatedQuestionEmbedding);

         //var unrelatedSimilarity = CalculateCosineSimilarity(memoryEmbedding, unrelatedQuestionEmbedding);

         //Console.WriteLine($"Related similarity:   {relatedSimilarity:F4}");

         //Console.WriteLine($"Unrelated similarity: {unrelatedSimilarity:F4}");

         var orchestrator = catalog.GetExportedValue<IConversationOrchestrator>();
         var app = new JarvisConsoleApplication(conversation, orchestrator);
         await PreloadModelAsync(httpClient, "qwen3:4b-instruct-2507-q4_K_M", CancellationToken.None);
         //await PreloadModelAsync(httpClient, "embeddinggemma", CancellationToken.None);
         await app.RunAsync(CancellationToken.None);
      }

      private static async Task PreloadModelAsync(HttpClient httpClient, string model, CancellationToken cancellationToken = default)
      {
         if (string.IsNullOrWhiteSpace(model))
         {
            throw new ArgumentException("The Ollama model name cannot be empty.", nameof(model));
         }

         using var response = await httpClient.PostAsJsonAsync(
            "http://192.168.0.69:11434/api/generate", new { model, keep_alive = -1 }, cancellationToken);

         var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

         if (!response.IsSuccessStatusCode)
         {
            throw new HttpRequestException(
               $"Ollama preload failed for model '{model}'. " + $"HTTP {(int)response.StatusCode} {response.StatusCode}. "
                                                              + $"Response: {responseBody}", inner: null, response.StatusCode);
         }
      }

      #endregion
   }
}