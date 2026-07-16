// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application;

using System.Text;

using Dadpul.Jarvis.Core.Conversation;
using Dadpul.Jarvis.Interfaces.Frontend;

public sealed class JarvisApplication
{
   #region Constants and Fields

   private readonly IConversationProvider conversationProvider;

   private readonly IReadOnlyList<IFrontend> frontends;

   private readonly IConversationOrchestrator orchestrator;

   #endregion

   #region Constructors and Destructors

   public JarvisApplication(IConversationOrchestrator orchestrator, IConversationProvider conversationProvider, IEnumerable<IFrontend> frontends)
   {
      this.orchestrator = orchestrator;
      this.conversationProvider = conversationProvider;
      this.frontends = frontends.ToList();
   }

   #endregion

   #region Public Methods and Operators

   public async Task RunAsync(CancellationToken cancellationToken)
   {
      Console.WriteLine("JARVIS is online.");

      if (frontends.Count == 0)
      {
         Console.WriteLine("No frontends are registered.");
         return;
      }

      foreach (var frontend in frontends)
      {
         Console.WriteLine($"Starting frontend: {frontend.Name}");
      }

      Console.WriteLine();

      var frontendTasks = frontends.Select(frontend => RunFrontendAsync(frontend, cancellationToken)).ToArray();

      await Task.WhenAll(frontendTasks);

      Console.WriteLine("JARVIS shutting down.");
   }

   #endregion

   #region Methods

   private async Task ProcessMessageAsync(IFrontend frontend, FrontendMessage message, CancellationToken cancellationToken)
   {
      var input = message.Content.Trim();

      if (string.IsNullOrWhiteSpace(input))
      {
         return;
      }

      var session = conversationProvider.GetConversation(frontend.Name, message.ConversationId);

      await session.Lock.WaitAsync(cancellationToken);

      try
      {
         await ProcessMessageAsync(frontend, message.ConversationId, session.Conversation, input, cancellationToken);
      }
      finally
      {
         session.Lock.Release();
      }
   }

   private async Task ProcessMessageAsync(IFrontend frontend, string conversationId, ChatConversation conversation, string input,
      CancellationToken cancellationToken)
   {
      conversation.AddUserMessage(input);

      await frontend.BeginResponseAsync(conversationId, cancellationToken);

      var responseBuilder = new StringBuilder();
      ChatMetrics? metrics = null;

      await foreach (var chunk in orchestrator.RespondAsync(conversation, cancellationToken))
      {
         if (!string.IsNullOrEmpty(chunk.Content))
         {
            await frontend.WriteResponseChunkAsync(conversationId, chunk.Content, cancellationToken);

            responseBuilder.Append(chunk.Content);
         }

         if (chunk.Metrics is not null)
         {
            metrics = chunk.Metrics;
         }
      }

      var responseContent = responseBuilder.ToString();

      if (!string.IsNullOrWhiteSpace(responseContent))
      {
         conversation.AddAssistantMessage(responseContent);
      }

      await frontend.CompleteResponseAsync(conversationId, metrics, cancellationToken);
   }

   private async Task RunFrontendAsync(IFrontend frontend, CancellationToken cancellationToken)
   {
      try
      {
         await frontend.RunAsync((message, messageCancellationToken) => ProcessMessageAsync(frontend, message, messageCancellationToken),
            cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         // Normal application shutdown.
      }
      catch (Exception exception)
      {
         Console.WriteLine($"Frontend '{frontend.Name}' failed: {exception}");
      }
   }

   #endregion
}

public sealed class ConversationSession
{
   #region Constructors and Destructors

   public ConversationSession(ChatConversation conversation)
   {
      ArgumentNullException.ThrowIfNull(conversation);

      Conversation = conversation;
   }

   #endregion

   #region Public Properties

   public ChatConversation Conversation { get; }

   public SemaphoreSlim Lock { get; } = new(1, 1);

   #endregion
}