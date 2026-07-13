// Bonjour

namespace Dadpul.Jarvis.Console
{
   using Dadpul.Jarvis.Console.Application;
   using Dadpul.Jarvis.Console.Application.Prompts;
   using Dadpul.Jarvis.Console.Chat;
   using Dadpul.Jarvis.Console.Conversation;
   using Dadpul.Jarvis.Console.Ollama;
   using Dadpul.Jarvis.Console.Tools;
   using Dadpul.Jarvis.Console.Tools.Lights;

   internal class Program
   {
      #region Methods

      static async Task Main(string[] args)
      {
         System.Console.InputEncoding = System.Text.Encoding.UTF8;
         System.Console.OutputEncoding = System.Text.Encoding.UTF8;

         var conversation = new ChatConversation();
         conversation.AddSystemMessage(JarvisSystemPrompt.Content);
         var ollamaOptions = new OllamaOptions { BaseAddress = new Uri("http://localhost:11434"), Model = "qwen3:8b" };

         using var httpClient = new HttpClient { BaseAddress = ollamaOptions.BaseAddress };

         IChatModel chatModel = new OllamaChatModel(httpClient, ollamaOptions);

         var registry = new ToolRegistry();
         var lights = new VirtualLight();
         var lightsOffTool = new TurnOffLightTool(lights);

         registry.Register(lightsOffTool);

         var orchestrator = new ConversationOrchestrator(chatModel, registry);

         var app = new JarvisConsoleApplication(conversation, orchestrator);

         await app.RunAsync(CancellationToken.None);
      }

      #endregion
   }
}