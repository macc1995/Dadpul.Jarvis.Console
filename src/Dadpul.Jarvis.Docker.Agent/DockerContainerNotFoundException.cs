// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Agent;

internal sealed class DockerContainerNotFoundException : Exception
{
   #region Constructors and Destructors

   public DockerContainerNotFoundException(string containerId)
      : base($"Docker container '{containerId}' was not found.")
   {
   }

   #endregion
}