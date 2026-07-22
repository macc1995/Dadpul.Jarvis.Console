// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal sealed class GitHubClientException : Exception
{
   #region Constructors and Destructors

   public GitHubClientException(string message)
      : base(message)
   {
   }

   public GitHubClientException(string message, Exception innerException)
      : base(message, innerException)
   {
   }

   #endregion
}
