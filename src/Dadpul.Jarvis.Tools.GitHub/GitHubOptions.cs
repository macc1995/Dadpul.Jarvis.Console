// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

public sealed class GitHubOptions
{
   #region Constants and Fields

   public const string SectionName = "GitHub";

   public const string TokenConfigurationKey = SectionName + ":Token";

   #endregion

   #region Public Properties

   public List<string> AllowedRepositories { get; set; } = [];

   public Uri? ApiBaseAddress { get; set; } = new("https://api.github.com/");

   public string ApiVersion { get; set; } = "2022-11-28";

   public string DefaultRepository { get; set; } = string.Empty;

   public int RequestTimeoutSeconds { get; set; } = 30;

   #endregion
}