// Made by Dadpul

using System.ComponentModel.Composition;

using Dadpul.Jarvis.Interfaces.Frontend;

[Export(typeof(IFrontend))]
public sealed class ConsoleFrontend : IFrontend
{
   #region IFrontend Members

   public string Name => "Console";

   public Task BeginResponseAsync(string conversationId, CancellationToken cancellationToken)
   {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.Write("JARVIS: ");
      Console.ForegroundColor = ConsoleColor.White;

      return Task.CompletedTask;
   }

   public Task CompleteResponseAsync(string conversationId, ChatMetrics? metrics, CancellationToken cancellationToken)
   {
      Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
      PrintMetrics(metrics);
        Console.ForegroundColor= ConsoleColor.White;
      Console.WriteLine();

      return Task.CompletedTask;
   }

   public async Task RunAsync(Func<FrontendMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(messageHandler);

      Console.WriteLine("Console frontend is online.");
      Console.WriteLine("Type /exit to close the console frontend.");
      Console.WriteLine();

      while (!cancellationToken.IsCancellationRequested)
      {
         Console.Write("You: ");

         string? input;

         try
         {
            input = await Console.In.ReadLineAsync(cancellationToken);
         }
         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
         {
            break;
         }

         if (input is null)
         {
            break;
         }

         input = input.Trim();

         if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
         {
            break;
         }

         if (string.IsNullOrWhiteSpace(input))
         {
            continue;
         }

         var message = new FrontendMessage(input, "console");

         await messageHandler(message, cancellationToken);
      }

      Console.WriteLine();
      Console.WriteLine("Console frontend shutting down.");
   }

   public Task WriteResponseChunkAsync(string conversationId, string content, CancellationToken cancellationToken)
   {
      if (!string.IsNullOrEmpty(content))
      {
            Console.ForegroundColor = ConsoleColor.Green;
         Console.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
      }

      return Task.CompletedTask;
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

      Console.WriteLine();
      Console.WriteLine("────────────────────────────────────────");
      Console.WriteLine($"Model:              {metrics.Model}");
      Console.WriteLine($"Finish reason:      {metrics.FinishReason ?? "unknown"}");
      Console.WriteLine();
      Console.WriteLine($"Prompt tokens:      {metrics.PromptTokenCount:N0}");
      Console.WriteLine($"Generated tokens:   {metrics.GeneratedTokenCount:N0}");
      Console.WriteLine();
      Console.WriteLine($"Model load:         {FormatDuration(metrics.LoadDuration)}");
      Console.WriteLine($"Prompt evaluation:  {FormatDuration(metrics.PromptEvaluationDuration)}");
      Console.WriteLine($"Token generation:   {FormatDuration(metrics.GenerationDuration)}");
      Console.WriteLine($"Total Ollama time:  {FormatDuration(metrics.TotalDuration)}");
      Console.WriteLine();
      Console.WriteLine($"Prompt speed:       {metrics.PromptTokensPerSecond:N2} tokens/s");
      Console.WriteLine($"Generation speed:   {metrics.GenerationTokensPerSecond:N2} tokens/s");
      Console.WriteLine("────────────────────────────────────────");
   }

   #endregion
}