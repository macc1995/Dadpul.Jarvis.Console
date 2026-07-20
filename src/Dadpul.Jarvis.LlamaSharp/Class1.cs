using Dadpul.Jarvis.Core.Chat;

namespace Dadpul.Jarvis.LlamaSharp
   
{
    using Dadpul.Jarvis.Core.Conversation;
    using LLama;
    using LLama.Common;
    public class Class1
    {

    }

    public sealed class LLamaSharpModelOptions
    {
        #region Public Properties

        public ChatModelCapabilities Capabilities { get; set; }
           = ChatModelCapabilities.ConversationOnly;

        public int ContextSize { get; set; } = 4096;

        public int GpuLayerCount { get; set; }

        public string ModelPath { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Priority { get; set; }

        public uint Seed { get; set; } = 1337;

        #endregion
    }

    public sealed class LLamaSharpChatModel : IChatModel, IDisposable
    {
        #region Fields

        private readonly SemaphoreSlim loadingLock = new(1, 1);

        private readonly LLamaSharpModelOptions options;

        private LLamaWeights? weights;

        private bool disposed;

        #endregion

        #region Constructors and Destructors

        public LLamaSharpChatModel(LLamaSharpModelOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
        }

        #endregion

        #region Public Properties

        public ChatModelDescriptor Descriptor =>
           new(
              options.Name,
              options.Capabilities,
              true);

        public int Priority => options.Priority;

        public async IAsyncEnumerable<ChatResponseChunk> GenerateResponseAsync(
   IReadOnlyList<ChatMessage> messages,
   IReadOnlyList<ChatToolDefinition> tools,
   [System.Runtime.CompilerServices.EnumeratorCancellation]
   CancellationToken cancellationToken)
        {
            await GetWeightsAsync(cancellationToken);

            yield return new ChatResponseChunk
            {
                Content = $"[{options.Name} loaded successfully]",
                Done = false
            };

            yield return new ChatResponseChunk
            {
                Done = true
            };
        }
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

                var modelParameters = new ModelParams(options.ModelPath)
                {
                    ContextSize = (uint)options.ContextSize,
                    GpuLayerCount = options.GpuLayerCount
                };

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

            bool available =
               !string.IsNullOrWhiteSpace(options.ModelPath)
               && File.Exists(options.ModelPath);

            return Task.FromResult(available);
        }

        #endregion
    }
}
