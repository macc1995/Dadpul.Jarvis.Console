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