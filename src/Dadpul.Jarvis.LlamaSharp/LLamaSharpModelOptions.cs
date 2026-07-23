// Made by Dadpul

namespace Dadpul.Jarvis.LlamaSharp;

using Dadpul.Jarvis.Core.Chat;

public sealed class LLamaSharpModelOptions
{
   #region Public Properties

   public ChatModelCapabilities Capabilities { get; set; } = ChatModelCapabilities.ConversationOnly;

   public int ContextSize { get; set; } = 4096;

   public int GpuLayerCount { get; set; }

   public int MinMemory { get; set; }

   public string ModelPath { get; set; } = string.Empty;

   public string Name { get; set; } = string.Empty;

   public int Priority { get; set; }

   public uint Seed { get; set; } = 1337;

   #endregion
}