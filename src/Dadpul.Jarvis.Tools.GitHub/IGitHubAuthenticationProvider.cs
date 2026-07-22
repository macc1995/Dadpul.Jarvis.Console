// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

internal interface IGitHubAuthenticationProvider
{
   #region Public Methods and Operators

   void Apply(HttpRequestMessage request);

   #endregion
}
