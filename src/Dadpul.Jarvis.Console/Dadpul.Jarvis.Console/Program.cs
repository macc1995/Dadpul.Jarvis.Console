// Made by Dadpul

namespace Dadpul.Jarvis.Console
{
   using System.ComponentModel.Composition;
   using System.ComponentModel.Composition.Hosting;
   using System.Net.Http.Json;
   using System.Reflection;

   using Dadpul.Jarvis.Core.Application;
   using Dadpul.Jarvis.Core.Application.Propmpts;
   using Dadpul.Jarvis.Core.Chat;
   using Dadpul.Jarvis.Core.Conversation;
   using Dadpul.Jarvis.Core.Ollama;
   using Dadpul.Jarvis.Discord;
   using Dadpul.Jarvis.Embeddings;
   using Dadpul.Jarvis.Interfaces.Frontend;
   using Dadpul.Jarvis.Interfaces.Tools;

   using Microsoft.Extensions.Configuration;

   using Console = System.Console;

   internal static class Program
   {
      #region Methods

      private static ChatConversation CreateConversation()
      {
         var conversation = new ChatConversation();

         conversation.AddSystemMessage(JarvisSystemPrompt.Content);

         return conversation;
      }

      private static async Task Main(string[] args)
      {
         Console.InputEncoding = System.Text.Encoding.UTF8;
         Console.OutputEncoding = System.Text.Encoding.UTF8;

         var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true).Build();
         using var cancellationTokenSource = new CancellationTokenSource();

         Console.CancelKeyPress += (_, eventArgs) =>
         {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
         };

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

         using var aggregateCatalog = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()),
            new DirectoryCatalog(assemblyDirectory, "Dadpul.Jarvis.Tools.*.dll"), new DirectoryCatalog(assemblyDirectory, "Dadpul.Jarvis.Core.dll"));

         using var compositionContainer = new CompositionContainer(aggregateCatalog);

         var compositionBatch = new CompositionBatch();

         compositionBatch.AddExportedValue(chatModel);
         compositionBatch.AddExportedValue(embeddingGenerator);
         compositionBatch.AddExportedValue("embeddingModel", ollamaOptions.EmbeddingModel);

         compositionContainer.Compose(compositionBatch);

         var registry = compositionContainer.GetExportedValue<IToolRegistry>();

         foreach (var tool in compositionContainer.GetExportedValues<ITool>())
         {
            Console.WriteLine($"registering {tool.Name} {tool.Version}");

            registry.Register(tool);
         }

         var orchestrator = compositionContainer.GetExportedValue<IConversationOrchestrator>();

         IConversationProvider conversationProvider = new InMemoryConversationProvider(CreateConversation);
         var discordToken = configuration["Discord:Token"];

         if (string.IsNullOrWhiteSpace(discordToken))
         {
            throw new InvalidOperationException("Discord:Token is missing from User Secrets.");
         }

         IReadOnlyList<IFrontend> frontends =
         [
            new ConsoleFrontend(),
            new DiscordFrontend(discordToken)
         ];

         var application = new JarvisApplication(orchestrator, conversationProvider, frontends);

         await PreloadModelAsync(httpClient, ollamaOptions.Model, cancellationTokenSource.Token);

         await application.RunAsync(cancellationTokenSource.Token);
      }

      private static async Task PreloadModelAsync(HttpClient httpClient, string model, CancellationToken cancellationToken = default)
      {
         if (string.IsNullOrWhiteSpace(model))
         {
            throw new ArgumentException("The Ollama model name cannot be empty.", nameof(model));
         }

         using var response = await httpClient.PostAsJsonAsync("api/generate", new { model, keep_alive = -1 }, cancellationToken);

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