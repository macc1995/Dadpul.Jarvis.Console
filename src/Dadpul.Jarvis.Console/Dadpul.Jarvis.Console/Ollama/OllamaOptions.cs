namespace Dadpul.Jarvis.Console.Ollama;

internal sealed class OllamaOptions
{
    public required Uri BaseAddress { get; init; }

    public required string Model { get; init; }
}