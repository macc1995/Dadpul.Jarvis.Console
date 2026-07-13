// Bonjour

namespace Dadpul.Jarvis.Console
{
   using System.ComponentModel.Composition.Hosting;
   using System.Reflection;

   using Dadpul.Jarvis.Console.Application;
   using Dadpul.Jarvis.Console.Application.Prompts;
   using Dadpul.Jarvis.Console.Chat;
   using Dadpul.Jarvis.Console.Conversation;
   using Dadpul.Jarvis.Console.Ollama;
   using Dadpul.Jarvis.Console.Tools;
   using Dadpul.Jarvis.Interfaces.Tools;

   using Console = System.Console;

   internal class Program
   {
      #region Methods

      static async Task Main(string[] args)
      {
         Console.InputEncoding = System.Text.Encoding.UTF8;
         Console.OutputEncoding = System.Text.Encoding.UTF8;

         var conversation = new ChatConversation();
         conversation.AddSystemMessage(JarvisSystemPrompt.Content);
         var ollamaOptions = new OllamaOptions { BaseAddress = new Uri("http://localhost:11434"), Model = "qwen3:8b" };

         using var httpClient = new HttpClient { BaseAddress = ollamaOptions.BaseAddress };

         IChatModel chatModel = new OllamaChatModel(httpClient, ollamaOptions);
         var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                 ?? throw new InvalidOperationException("Unable to resolve application directory.");

         //using var container = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()),
         // new DirectoryCatalog(assemblyDirectory, "Dadpul.Jarvis.Tools.*.dll"));
         using var container = new AggregateCatalog(new DirectoryCatalog(assemblyDirectory));
         using var catalog = new CompositionContainer(container);
         var registry = catalog.GetExportedValue<IToolRegistry>();

         foreach (var tool in catalog.GetExportedValues<ITool>())
         {
            Console.WriteLine($"registering {tool.Name}");
            registry.Register(tool);
         }

         var orchestrator = new ConversationOrchestrator(chatModel, registry);

         var app = new JarvisConsoleApplication(conversation, orchestrator);

         await app.RunAsync(CancellationToken.None);
      }

      #endregion
   }
}