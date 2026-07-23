// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Net.Http.Headers;

using Microsoft.Extensions.Configuration;

[Export(typeof(IGitHubAuthenticationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class PersonalAccessTokenAuthenticationProvider : IGitHubAuthenticationProvider
{
   #region Constants and Fields

   private readonly string? token;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public PersonalAccessTokenAuthenticationProvider(IConfiguration configuration)
   {
      ArgumentNullException.ThrowIfNull(configuration);

      token = configuration[GitHubOptions.TokenConfigurationKey]?.Trim();
   }

   #endregion

   #region IGitHubAuthenticationProvider Members

   public void Apply(HttpRequestMessage request)
   {
      ArgumentNullException.ThrowIfNull(request);

      if (string.IsNullOrWhiteSpace(token))
      {
         throw new GitHubClientException($"GitHub authentication is not configured. Add '{GitHubOptions.TokenConfigurationKey}' to User Secrets.");
      }

      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
   }

   #endregion
}