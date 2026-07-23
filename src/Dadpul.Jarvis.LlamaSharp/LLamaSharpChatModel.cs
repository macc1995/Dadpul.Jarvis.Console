// Made by Dadpul

namespace Dadpul.Jarvis.LlamaSharp

{
   using System.Runtime.CompilerServices;

   using Dadpul.Jarvis.Core.Application.Propmpts;
   using Dadpul.Jarvis.Core.Chat;
   using Dadpul.Jarvis.Core.Conversation;

   using LLama;
   using LLama.Common;
   using LLama.Sampling;
   using LLama.Transformers;

   public sealed class LLamaSharpChatModel : IChatModel, IDisposable
   {
      #region Constants and Fields

      private readonly string modelPath;

      ISystemPrompt systemPrompt;

      #endregion

      #region Constructors and Destructors

      public LLamaSharpChatModel(LLamaSharpModelOptions options, ISystemPrompt systemPrompt)
      {
         this.systemPrompt = systemPrompt;
         ArgumentNullException.ThrowIfNull(options);

         this.options = options;
         modelPath = Path.IsPathRooted(options.ModelPath) ? options.ModelPath : Path.GetFullPath(options.ModelPath, AppContext.BaseDirectory);
      }

      #endregion

      #region IChatModel Members

      public ChatModelDescriptor Descriptor => new(options.Name, options.Capabilities, true);

      public int Priority => options.Priority;

      public ISystemPrompt SystemPrompt => systemPrompt;

      public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(IReadOnlyList<ChatMessage> messages,
         IReadOnlyList<ChatToolDefinition> tools, [EnumeratorCancellation] CancellationToken cancellationToken)
      {
         var model = await GetWeightsAsync(cancellationToken);

         var modelParameters = CreateModelParameters();

         using var context = model.CreateContext(modelParameters);

         var executor = new InteractiveExecutor(context);

         var history = CreateChatHistory(messages);

         var session = new ChatSession(executor);

         session.WithHistoryTransform(new PromptTemplateTransformer(model, withAssistant: true));

         var inferenceParameters = new InferenceParams { MaxTokens = 512, SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.6f } };

         await foreach (var content in session.ChatAsync(history, inferenceParameters, cancellationToken))
         {
            if (string.IsNullOrEmpty(content))
            {
               continue;
            }

            yield return new ChatResponseChunk { Content = content };
         }

         yield return new ChatResponseChunk { Done = true };
      }

      public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         if (disposed || string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
         {
            return Task.FromResult(false);
         }

         if (weights is not null)
         {
            return Task.FromResult(true);
         }

         if (options.MinMemory <= 0)
         {
            return Task.FromResult(true);
         }

         var availableMemory = GetAvailableMemoryInMegabytes();

         return Task.FromResult(availableMemory >= options.MinMemory);
      }

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
         if (disposed)
         {
            return;
         }

         disposed = true;

         weights?.Dispose();
         loadingLock.Dispose();
      }

      #endregion

      #region Methods

      private static ChatHistory CreateChatHistory(IReadOnlyList<ChatMessage> messages)
      {
         var history = new ChatHistory();

         foreach (var message in messages)
         {
            switch (message.Role)
            {
               case ChatRole.System:
                  history.AddMessage(AuthorRole.System, message.Content);
                  break;

               case ChatRole.User:
                  history.AddMessage(AuthorRole.User, message.Content);
                  break;

               case ChatRole.Assistant:
                  history.AddMessage(AuthorRole.Assistant, message.Content);
                  break;

               case ChatRole.Tool:
                  // ConversationOnly models cannot represent tool messages.
                  break;

               default:
                  throw new ArgumentOutOfRangeException(nameof(message.Role), message.Role, "Unsupported chat role.");
            }
         }

         if (history.Messages.Count == 0)
         {
            throw new InvalidOperationException("The chat history is empty.");
         }

         if (history.Messages[^1].AuthorRole != AuthorRole.User)
         {
            throw new InvalidOperationException("The final message must be a user message.");
         }

         return history;
      }

      private static long GetAvailableMemoryInMegabytes()
      {
         var memoryInfo = GC.GetGCMemoryInfo();

         var totalMemory = memoryInfo.TotalAvailableMemoryBytes;
         var highMemoryThreshold = memoryInfo.HighMemoryLoadThresholdBytes;

         long safeMemoryLimit;

         if ((totalMemory > 0) && (highMemoryThreshold > 0))
         {
            safeMemoryLimit = Math.Min(totalMemory, highMemoryThreshold);
         }
         else
         {
            safeMemoryLimit = Math.Max(totalMemory, highMemoryThreshold);
         }

         if (safeMemoryLimit <= 0)
         {
            // Do not reject every model merely because memory information
            // was unavailable.
            return long.MaxValue;
         }

         var availableMemory = Math.Max(0, safeMemoryLimit - memoryInfo.MemoryLoadBytes);

         return availableMemory / (1024L * 1024L);
      }

      private ModelParams CreateModelParameters()
      {
         return new ModelParams(options.ModelPath) { ContextSize = (uint)options.ContextSize, GpuLayerCount = options.GpuLayerCount };
      }

      private async Task<LLamaWeights> GetWeightsAsync(CancellationToken cancellationToken)
      {
         ObjectDisposedException.ThrowIf(disposed, this);

         if (weights is not null)
         {
            return weights;
         }

         await loadingLock.WaitAsync(cancellationToken);

         try
         {
            if (weights is not null)
            {
               return weights;
            }

            var modelParameters = CreateModelParameters();

            weights = await Task.Run(() => LLamaWeights.LoadFromFile(modelParameters), cancellationToken);

            return weights;
         }
         finally
         {
            loadingLock.Release();
         }
      }

      #endregion

      #region Fields

      private readonly SemaphoreSlim loadingLock = new(1, 1);

      private readonly LLamaSharpModelOptions options;

      private LLamaWeights? weights;

      private bool disposed;

      #endregion
   }
}