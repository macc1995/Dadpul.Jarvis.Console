// Made by Dadpul

namespace Dadpul.Jarvis.Discord;

public sealed class DiscordOptions
{
    #region Constants and Fields

    public const string SectionName = "Discord";

    #endregion

    #region Public Properties

    public bool Enabled { get; set; } = true;

    public string Token { get; set; } = string.Empty;

    #endregion
}