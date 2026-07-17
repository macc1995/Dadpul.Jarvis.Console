// Made by Dadpul

namespace Dadpul.Jarvis.Core.Ollama;

public sealed class OllamaOptions
{
    #region Public Properties

    public static  string SectionName { get; } = "Ollama";

    public Uri? BaseAddress { get; set; }

    public string EmbeddingModel { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool Think { get; set; }

    public OllamaPreloadOptions Preload { get; set; } = new();

    #endregion
}
public sealed class OllamaPreloadOptions
{
    #region Public Properties

    public bool Enabled { get; set; } = true;

    public int KeepAlive { get; set; } = -1;

    #endregion
}