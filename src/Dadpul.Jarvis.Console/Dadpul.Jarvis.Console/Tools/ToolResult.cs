// Bonjour

namespace Dadpul.Jarvis.Console.Tools;

internal sealed class ToolResult
{
   #region Public Properties

   public required string Content { get; init; }

   public required bool Success { get; init; }

   #endregion

   #region Public Methods and Operators

   public static ToolResult Failed(string content)
   {
      return new ToolResult { Success = false, Content = content };
   }

   public static ToolResult Successful(string content)
   {
      return new ToolResult { Success = true, Content = content };
   }

   #endregion
}