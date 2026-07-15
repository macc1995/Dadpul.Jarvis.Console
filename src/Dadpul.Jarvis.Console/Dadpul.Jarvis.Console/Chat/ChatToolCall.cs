// Made by Dadpul

namespace Dadpul.Jarvis.Console.Chat;

using System.Text.Json.Nodes;

internal sealed class ChatToolCall
{
   #region Public Properties

   public required JsonObject Arguments { get; init; }

   public required string Name { get; init; }

   #endregion
}