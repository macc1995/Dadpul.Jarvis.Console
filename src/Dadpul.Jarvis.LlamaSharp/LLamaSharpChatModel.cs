using Dadpul.Jarvis.Core.Chat;

namespace Dadpul.Jarvis.LlamaSharp
   
{
    using Dadpul.Jarvis.Core.Conversation;
    using LLama;
    using LLama.Common;
    using System.Runtime.CompilerServices;

    using LLama.Sampling;
    using LLama.Transformers;
    using Dadpul.Jarvis.Core.Application.Propmpts;

    public sealed class LLamaSharpChatModel : IChatModel, IDisposable
    {
        private static ChatHistory CreateChatHistory(
   IReadOnlyList<ChatMessage> messages)
        {
            ChatHistory history = new ChatHistory();

            foreach (ChatMessage message in messages)
            {
                switch (message.Role)
                {
                    case ChatRole.System:
                        history.AddMessage(
                           AuthorRole.System,
                           message.Content);
                        break;

                    case ChatRole.User:
                        history.AddMessage(
                           AuthorRole.User,
                           message.Content);
                        break;

                    case ChatRole.Assistant:
                        history.AddMessage(
                           AuthorRole.Assistant,
                           message.Content);
                        break;

                    case ChatRole.Tool:
                        // ConversationOnly models cannot represent tool messages.
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                           nameof(message.Role),
                           message.Role,
                           "Unsupported chat role.");
                }
            }

            if (history.Messages.Count == 0)
            {
                throw new InvalidOperationException(
                   "The chat history is empty.");
            }

            if (history.Messages[^1].AuthorRole != AuthorRole.User)
            {
                throw new InvalidOperationException(
                   "The final message must be a user message.");
            }

            return history;
        }
        private static long GetAvailableMemoryInMegabytes()
        {
            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

            long totalMemory = memoryInfo.TotalAvailableMemoryBytes;
            long highMemoryThreshold = memoryInfo.HighMemoryLoadThresholdBytes;

            long safeMemoryLimit;

            if (totalMemory > 0 && highMemoryThreshold > 0)
            {
                safeMemoryLimit = Math.Min(
                   totalMemory,
                   highMemoryThreshold);
            }
            else
            {
                safeMemoryLimit = Math.Max(
                   totalMemory,
                   highMemoryThreshold);
            }

            if (safeMemoryLimit <= 0)
            {
                // Do not reject every model merely because memory information
                // was unavailable.
                return long.MaxValue;
            }

            long availableMemory = Math.Max(
               0,
               safeMemoryLimit - memoryInfo.MemoryLoadBytes);

            return availableMemory / (1024L * 1024L);
        }
        #region Fields

        private readonly SemaphoreSlim loadingLock = new(1, 1);

        private readonly LLamaSharpModelOptions options;

        private LLamaWeights? weights;

        private bool disposed;

        #endregion

        #region Constructors and Destructors
        ISystemPrompt systemPrompt;
        public LLamaSharpChatModel(LLamaSharpModelOptions options, ISystemPrompt systemPrompt)
        {
            this.systemPrompt = systemPrompt;
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
            modelPath = Path.IsPathRooted(options.ModelPath)
     ? options.ModelPath
     : Path.GetFullPath(
        options.ModelPath,
        AppContext.BaseDirectory);
        }

        #endregion

        #region Public Properties

        public ChatModelDescriptor Descriptor =>
           new(
              options.Name,
              options.Capabilities,
              true);

        public int Priority => options.Priority;

        public ISystemPrompt SystemPrompt => systemPrompt;

        public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(
   IReadOnlyList<ChatMessage> messages,
   IReadOnlyList<ChatToolDefinition> tools,
   [EnumeratorCancellation]
   CancellationToken cancellationToken)
        {
            LLamaWeights model =
               await GetWeightsAsync(cancellationToken);

            ModelParams modelParameters =
               CreateModelParameters();

            using LLamaContext context =
               model.CreateContext(modelParameters);

            InteractiveExecutor executor =
               new InteractiveExecutor(context);

            ChatHistory history =
               CreateChatHistory(messages);

            ChatSession session =
               new ChatSession(executor);

            session.WithHistoryTransform(
               new PromptTemplateTransformer(
                  model,
                  withAssistant: true));

            InferenceParams inferenceParameters =
               new InferenceParams
               {
                   MaxTokens = 512,

                   SamplingPipeline =
                     new DefaultSamplingPipeline
                     {
                         Temperature = 0.6f
                     }
               };

            await foreach (
               string content in session.ChatAsync(
                  history,
                  inferenceParameters,
                  cancellationToken))
            {
                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                yield return new ChatResponseChunk
                {
                    Content = content
                };
            }

            yield return new ChatResponseChunk
            {
                Done = true
            };
        }

        private readonly string modelPath;


        private async Task<LLamaWeights> GetWeightsAsync(
   CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(
               disposed,
               this);

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

                weights = await Task.Run(
                   () => LLamaWeights.LoadFromFile(modelParameters),
                   cancellationToken);

                return weights;
            }
            finally
            {
                loadingLock.Release();
            }
        }

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


        public Task<bool> IsAvailableAsync(
   CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (disposed
                || string.IsNullOrWhiteSpace(modelPath)
                || !File.Exists(modelPath))
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

            long availableMemory = GetAvailableMemoryInMegabytes();

            return Task.FromResult(
               availableMemory >= options.MinMemory);
        }
        private ModelParams CreateModelParameters()
        {
            return new ModelParams(options.ModelPath)
            {
                ContextSize = (uint)options.ContextSize,
                GpuLayerCount = options.GpuLayerCount
            };
        }
        #endregion
    }

    public class LlamaSharpSystemPrompt
    {
        public string Prompt { get; } = """

            """;
    }
}
