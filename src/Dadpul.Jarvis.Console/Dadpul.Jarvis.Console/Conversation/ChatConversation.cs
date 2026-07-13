using Dadpul.Jarvis.Console.Chat;

namespace Dadpul.Jarvis.Console.Conversation;

internal sealed class ChatConversation
{
    private readonly List<ChatMessage> messages = [];

    public IReadOnlyList<ChatMessage> Messages => messages;

    public void AddUserMessage(string content)
    {
        AddMessage(ChatRole.User, content);
    }

    public void AddAssistantMessage(string content)
    {
        AddMessage(ChatRole.Assistant, content);
    }
    public void AddAssistantToolCallMessage(
    string content,
    IReadOnlyList<ChatToolCall> toolCalls)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        if (toolCalls.Count == 0)
        {
            throw new ArgumentException(
                "At least one tool call is required.",
                nameof(toolCalls));
        }

        messages.Add(new ChatMessage(
            ChatRole.Assistant,
            content,
            toolCalls));
    }

    public void AddToolResultMessage(
        string toolName,
        string content)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException(
                "A tool name is required.",
                nameof(toolName));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "A tool result cannot be empty.",
                nameof(content));
        }

        messages.Add(new ChatMessage(
            ChatRole.Tool,
            content.Trim(),
            ToolName: toolName));
    }
    public void AddSystemMessage(string content)
    {
        AddMessage(ChatRole.System, content);
    }

    private void AddMessage(ChatRole role, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "A chat message cannot be empty.",
                nameof(content));
        }

        messages.Add(new ChatMessage(role, content.Trim()));
    }
}