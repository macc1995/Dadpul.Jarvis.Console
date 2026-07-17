// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

public sealed class DockerOptions
{
    #region Constants and Fields

    public const string SectionName = "Docker";

    #endregion

    #region Public Properties

    public Dictionary<string, DockerNodeOptions> Nodes { get; set; } = [];

    public int RequestTimeoutSeconds { get; set; } = 30;

    #endregion
}

// Made by Dadpul


public sealed class DockerNodeOptions
{
    #region Public Properties

    public string ApiKey { get; set; } = string.Empty;

    public Uri? BaseUrl { get; set; }

    public string Name { get; set; } = string.Empty;

    #endregion
}