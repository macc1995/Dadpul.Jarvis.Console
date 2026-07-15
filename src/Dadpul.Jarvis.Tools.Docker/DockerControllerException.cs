// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

internal sealed class DockerControllerException : Exception
{
   #region Constructors and Destructors

   public DockerControllerException(string message)
      : base(message)
   {
   }

   #endregion
}