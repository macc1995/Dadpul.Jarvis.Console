// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

public sealed class DockerNodeOptions
{
   #region Public Properties

   public string ApiKey { get; set; } = string.Empty;

   public Uri? BaseUrl { get; set; }

   public string Name { get; set; } = string.Empty;

   #endregion
}