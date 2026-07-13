// Bonjour

namespace Dadpul.Jarvis.Console.Tools;

using System.Text.Json.Nodes;

internal interface ITool
{
   #region Public Properties

   string Description { get; }

   string Name { get; }

   JsonObject Parameters { get; }

   #endregion

   #region Public Methods and Operators

   Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken);

   #endregion
}