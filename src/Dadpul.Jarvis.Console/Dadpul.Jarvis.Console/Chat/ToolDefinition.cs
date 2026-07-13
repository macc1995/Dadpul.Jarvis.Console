// Bonjour

namespace Dadpul.Jarvis.Console.Chat;

using System.Text.Json.Nodes;

internal sealed class ChatToolDefinition
{
   #region Public Properties

   public required string Description { get; init; }

   public required string Name { get; init; }

   public required JsonObject Parameters { get; init; }

   #endregion
}