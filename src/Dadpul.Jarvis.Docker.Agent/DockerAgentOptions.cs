// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Agent;

public sealed class DockerAgentOptions
{
   #region Constants and Fields

   public const string SectionName = "DockerAgent";

   #endregion

   #region Public Properties

   public string ApiKey { get; init; } = string.Empty;

   public string Endpoint { get; init; } = OperatingSystem.IsWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";

   public string ManagementLabel { get; init; } = "jarvis.manage";

   public string ManagementLabelValue { get; init; } = "true";

   public string NodeName { get; init; } = Environment.MachineName;

   public int RestartTimeoutSeconds { get; init; } = 10;

   public int StopTimeoutSeconds { get; init; } = 10;

   #endregion
}