// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat;

using System.Text.Json.Nodes;

public sealed class ChatToolCall
{
   #region Public Properties

   public required JsonObject Arguments { get; init; }

   public required string Name { get; init; }

   #endregion
}