// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Text.Json.Serialization;

[Export(typeof(IGitHubRepositoryService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class GitHubRepositoryService : IGitHubRepositoryService
{
   #region Constants and Fields

   private readonly IGitHubApiClient apiClient;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubRepositoryService(IGitHubApiClient apiClient)
   {
      this.apiClient = apiClient;
   }

   #endregion

   #region IGitHubRepositoryService Members

   public async Task<GitHubRepositoryInfo> GetRepositoryAsync(string? repository, CancellationToken cancellationToken)
   {
      var selectedRepository = apiClient.ResolveRepository(repository);
      var segments = selectedRepository.Split('/');
      var response = await apiClient.GetAsync<GitHubRepositoryResponse>(selectedRepository,
         $"repos/{Uri.EscapeDataString(segments[0])}/{Uri.EscapeDataString(segments[1])}", cancellationToken);

      if (string.IsNullOrWhiteSpace(response.FullName) || string.IsNullOrWhiteSpace(response.DefaultBranch)
                                                       || string.IsNullOrWhiteSpace(response.HtmlUrl)
                                                       || string.IsNullOrWhiteSpace(response.Owner?.Login))
      {
         throw new GitHubClientException($"GitHub returned an incomplete repository response for '{selectedRepository}'.");
      }

      return new GitHubRepositoryInfo(response.FullName, response.Owner.Login, response.DefaultBranch,
         response.Visibility ?? (response.Private ? "private" : "public"), response.Private, response.Archived, response.Description,
         response.HtmlUrl);
   }

   #endregion

   #region Nested Types

   private sealed class GitHubOwnerResponse
   {
      #region Public Properties

      [JsonPropertyName("login")] public string? Login { get; init; }

      #endregion
   }

   private sealed class GitHubRepositoryResponse
   {
      #region Public Properties

      [JsonPropertyName("archived")] public bool Archived { get; init; }

      [JsonPropertyName("default_branch")] public string? DefaultBranch { get; init; }

      [JsonPropertyName("description")] public string? Description { get; init; }

      [JsonPropertyName("full_name")] public string? FullName { get; init; }

      [JsonPropertyName("html_url")] public string? HtmlUrl { get; init; }

      [JsonPropertyName("owner")] public GitHubOwnerResponse? Owner { get; init; }

      [JsonPropertyName("private")] public bool Private { get; init; }

      [JsonPropertyName("visibility")] public string? Visibility { get; init; }

      #endregion
   }

   #endregion
}