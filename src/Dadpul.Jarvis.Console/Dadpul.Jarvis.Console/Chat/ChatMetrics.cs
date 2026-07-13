internal sealed class ChatResponse
{
    public required string Content { get; init; }

    public ChatMetrics? Metrics { get; init; }
}


internal sealed class ChatMetrics
{
    public required string Model { get; init; }

    public string? FinishReason { get; init; }

    public int PromptTokenCount { get; init; }

    public int GeneratedTokenCount { get; init; }

    public TimeSpan LoadDuration { get; init; }

    public TimeSpan PromptEvaluationDuration { get; init; }

    public TimeSpan GenerationDuration { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double PromptTokensPerSecond =>
        CalculateTokensPerSecond(
            PromptTokenCount,
            PromptEvaluationDuration);

    public double GenerationTokensPerSecond =>
        CalculateTokensPerSecond(
            GeneratedTokenCount,
            GenerationDuration);

    private static double CalculateTokensPerSecond(
        int tokenCount,
        TimeSpan duration)
    {
        if (tokenCount == 0 || duration <= TimeSpan.Zero)
        {
            return 0;
        }

        return tokenCount / duration.TotalSeconds;
    }
}