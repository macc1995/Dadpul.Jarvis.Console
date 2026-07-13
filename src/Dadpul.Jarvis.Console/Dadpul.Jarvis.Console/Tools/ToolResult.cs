namespace Dadpul.Jarvis.Console.Tools;

internal sealed class ToolResult
{
    public required bool Success { get; init; }

    public required string Content { get; init; }

    public static ToolResult Successful(string content)
    {
        return new ToolResult
        {
            Success = true,
            Content = content
        };
    }

    public static ToolResult Failed(string content)
    {
        return new ToolResult
        {
            Success = false,
            Content = content
        };
    }
}