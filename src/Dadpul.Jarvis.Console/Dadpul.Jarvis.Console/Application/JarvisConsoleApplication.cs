// Made by Dadpul

namespace Dadpul.Jarvis.Console.Application;

using System.Text;

using Dadpul.Jarvis.Console.Conversation;

internal sealed class JarvisConsoleApplication
{
   #region Constants and Fields

   private readonly ChatConversation conversation;

   private readonly IConversationOrchestrator orchestrator;

   #endregion

   #region Constructors and Destructors

   public JarvisConsoleApplication(ChatConversation conversation, IConversationOrchestrator orchestrator)
   {
      this.conversation = conversation;
      this.orchestrator = orchestrator;
   }

   #endregion

   #region Public Methods and Operators

   public async Task RunAsync(CancellationToken cancellationToken)
   {
      System.Console.WriteLine("JARVIS is online.");
      System.Console.WriteLine("Type /exit to shut down.");
      System.Console.WriteLine();

      while (!cancellationToken.IsCancellationRequested)
      {
         System.Console.Write("You: ");

         var input = System.Console.ReadLine();

         if (input is null)
         {
            break;
         }

         input = input.Trim();

         if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
         {
            break;
         }

         if (input.Equals("/history", StringComparison.OrdinalIgnoreCase))
         {
            PrintHistory();
            continue;
         }

         if (string.IsNullOrWhiteSpace(input))
         {
            continue;
         }

         conversation.AddUserMessage(input);

         System.Console.Write("JARVIS: ");

         var responseBuilder = new StringBuilder();
         ChatMetrics? metrics = null;

         await foreach (var chunk in orchestrator.RespondAsync(conversation, cancellationToken))
         {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
               System.Console.Write(chunk.Content);
               responseBuilder.Append(chunk.Content);
            }

            if (chunk.Metrics is not null)
            {
               metrics = chunk.Metrics;
            }
         }

         System.Console.WriteLine();

         var responseContent = responseBuilder.ToString();

         if (!string.IsNullOrWhiteSpace(responseContent))
         {
            conversation.AddAssistantMessage(responseContent);
         }

         PrintMetrics(metrics);

         System.Console.WriteLine();
      }

      System.Console.WriteLine("JARVIS shutting down.");

      await Task.CompletedTask;
   }

   #endregion

   #region Methods

   private static string FormatDuration(TimeSpan duration)
   {
      if (duration.TotalSeconds >= 1)
      {
         return $"{duration.TotalSeconds:N2} s";
      }

      return $"{duration.TotalMilliseconds:N2} ms";
   }

   private static void PrintMetrics(ChatMetrics? metrics)
   {
      if (metrics is null)
      {
         return;
      }

      System.Console.WriteLine();
      System.Console.WriteLine("────────────────────────────────────────");

      System.Console.WriteLine($"Model:              {metrics.Model}");

      System.Console.WriteLine($"Finish reason:      {metrics.FinishReason ?? "unknown"}");

      System.Console.WriteLine();

      System.Console.WriteLine($"Prompt tokens:      {metrics.PromptTokenCount:N0}");

      System.Console.WriteLine($"Generated tokens:   {metrics.GeneratedTokenCount:N0}");

      System.Console.WriteLine();

      System.Console.WriteLine($"Model load:         {FormatDuration(metrics.LoadDuration)}");

      System.Console.WriteLine($"Prompt evaluation:  {FormatDuration(metrics.PromptEvaluationDuration)}");

      System.Console.WriteLine($"Token generation:   {FormatDuration(metrics.GenerationDuration)}");

      System.Console.WriteLine($"Total Ollama time:  {FormatDuration(metrics.TotalDuration)}");

      System.Console.WriteLine();

      System.Console.WriteLine($"Prompt speed:       {metrics.PromptTokensPerSecond:N2} tokens/s");

      System.Console.WriteLine($"Generation speed:   {metrics.GenerationTokensPerSecond:N2} tokens/s");

      System.Console.WriteLine("────────────────────────────────────────");
   }

   private void PrintHistory()
   {
      System.Console.WriteLine();

      foreach (var message in conversation.Messages.Where(message => message.Role != ChatRole.System))
      {
         System.Console.WriteLine($"{message.Role}: {message.Content}");
      }

      System.Console.WriteLine();
   }

   #endregion
}