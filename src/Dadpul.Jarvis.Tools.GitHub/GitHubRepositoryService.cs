// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

[Export(typeof(IGitHubRepositoryService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class GitHubRepositoryService : IGitHubRepositoryService, IDisposable
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions JsonOptions =
      new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private readonly HashSet<string> allowedRepositories;

   private readonly IGitHubAuthenticationProvider authenticationProvider;

   private readonly string defaultRepository;

   private readonly HttpClient httpClient;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubRepositoryService(
      IConfiguration configuration,
      IGitHubAuthenticationProvider authenticationProvider)
   {
      ArgumentNullException.ThrowIfNull(configuration);
      ArgumentNullException.ThrowIfNull(authenticationProvider);

      var options = configuration
         .GetRequiredSection(GitHubOptions.SectionName)
         .Get<GitHubOptions>()
         ?? throw new GitHubClientException("GitHub configuration could not be bound.");

      ValidateOptions(options);

      this.authenticationProvider = authenticationProvider;
      defaultRepository = NormalizeRepository(options.DefaultRepository);
      allowedRepositories = options.AllowedRepositories
         .Select(NormalizeRepository)
         .ToHashSet(StringComparer.OrdinalIgnoreCase);

      if (!allowedRepositories.Contains(defaultRepository))
      {
         throw new GitHubClientException(
            $"GitHub:DefaultRepository '{defaultRepository}' must also be present in GitHub:AllowedRepositories.");
      }

      httpClient = new HttpClient
      {
         BaseAddress = EnsureTrailingSlash(options.ApiBaseAddress!),
         Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds)
      };

      httpClient.DefaultRequestHeaders.Accept.Add(
         new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
      httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Dadpul-Jarvis/0.1");
      httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", options.ApiVersion);
   }

   #endregion

   #region IDisposable Members

   public void Dispose()
   {
      httpClient.Dispose();
   }

   #endregion

   #region IGitHubRepositoryService Members

   public async Task<GitHubRepositoryInfo> GetRepositoryAsync(
      string? repository,
      CancellationToken cancellationToken)
   {
      var selectedRepository = string.IsNullOrWhiteSpace(repository)
                                  ? defaultRepository
                                  : NormalizeRepository(repository);

      EnsureAllowed(selectedRepository);

      var segments = selectedRepository.Split('/');
      var requestUri = $"repos/{Uri.EscapeDataString(segments[0])}/{Uri.EscapeDataString(segments[1])}";

      using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
      authenticationProvider.Apply(request);

      using var response = await httpClient.SendAsync(
         request,
         HttpCompletionOption.ResponseHeadersRead,
         cancellationToken);

      await EnsureSuccessfulAsync(selectedRepository, response, cancellationToken);

      GitHubRepositoryResponse? repositoryResponse;

      try
      {
         repositoryResponse = await response.Content.ReadFromJsonAsync<GitHubRepositoryResponse>(
            JsonOptions,
            cancellationToken);
      }
      catch (JsonException exception)
      {
         throw new GitHubClientException(
            $"GitHub returned an invalid repository response for '{selectedRepository}'.",
            exception);
      }

      if (repositoryResponse is null
          || string.IsNullOrWhiteSpace(repositoryResponse.FullName)
          || string.IsNullOrWhiteSpace(repositoryResponse.DefaultBranch)
          || string.IsNullOrWhiteSpace(repositoryResponse.HtmlUrl)
          || string.IsNullOrWhiteSpace(repositoryResponse.Owner?.Login))
      {
         throw new GitHubClientException(
            $"GitHub returned an incomplete repository response for '{selectedRepository}'.");
      }

      return new GitHubRepositoryInfo(
         repositoryResponse.FullName,
         repositoryResponse.Owner.Login,
         repositoryResponse.DefaultBranch,
         repositoryResponse.Visibility ?? (repositoryResponse.Private ? "private" : "public"),
         repositoryResponse.Private,
         repositoryResponse.Archived,
         repositoryResponse.Description,
         repositoryResponse.HtmlUrl);
   }

   #endregion

   #region Methods

   private static Uri EnsureTrailingSlash(Uri uri)
   {
      var value = uri.AbsoluteUri;

      return value.EndsWith('/', StringComparison.Ordinal)
                ? uri
                : new Uri(value + '/', UriKind.Absolute);
   }

   private static async Task EnsureSuccessfulAsync(
      string repository,
      HttpResponseMessage response,
      CancellationToken cancellationToken)
   {
      if (response.IsSuccessStatusCode)
      {
         return;
      }

      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
      var safeBody = responseBody.Length <= 1_000
                        ? responseBody
                        : responseBody[..1_000] + "…";

      throw new GitHubClientException(
         $"GitHub returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) "
         + $"for repository '{repository}'. Response: {safeBody}");
   }

   private static string NormalizeRepository(string repository)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(repository);

      var normalized = repository.Trim().Trim('/');
      var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      if (segments.Length != 2
          || segments.Any(segment => segment.Length == 0 || segment.Any(char.IsWhiteSpace)))
      {
         throw new GitHubClientException(
            $"Repository '{repository}' must use the 'owner/name' format.");
      }

      return $"{segments[0]}/{segments[1]}";
   }

   private static void ValidateOptions(GitHubOptions options)
   {
      if (options.ApiBaseAddress is null)
      {
         throw new GitHubClientException("GitHub:ApiBaseAddress is required.");
      }

      if (options.ApiBaseAddress.Scheme != Uri.UriSchemeHttps
          && options.ApiBaseAddress.Scheme != Uri.UriSchemeHttp)
      {
         throw new GitHubClientException("GitHub:ApiBaseAddress must use HTTP or HTTPS.");
      }

      if (string.IsNullOrWhiteSpace(options.ApiVersion))
      {
         throw new GitHubClientException("GitHub:ApiVersion is required.");
      }

      if (string.IsNullOrWhiteSpace(options.DefaultRepository))
      {
         throw new GitHubClientException("GitHub:DefaultRepository is required.");
      }

      if (options.AllowedRepositories.Count == 0)
      {
         throw new GitHubClientException("At least one GitHub:AllowedRepositories entry is required.");
      }

      if (options.RequestTimeoutSeconds <= 0)
      {
         throw new GitHubClientException("GitHub:RequestTimeoutSeconds must be greater than zero.");
      }
   }

   private void EnsureAllowed(string repository)
   {
      if (allowedRepositories.Contains(repository))
      {
         return;
      }

      throw new GitHubClientException(
         $"Repository '{repository}' is not allowed. Configured repositories: "
         + string.Join(", ", allowedRepositories.OrderBy(value => value)) + ".");
   }

   #endregion

   #region Nested Types

   private sealed class GitHubOwnerResponse
   {
      #region Public Properties

      [JsonPropertyName("login")]
      public string? Login { get; init; }

      #endregion
   }

   private sealed class GitHubRepositoryResponse
   {
      #region Public Properties

      [JsonPropertyName("archived")]
      public bool Archived { get; init; }

      [JsonPropertyName("default_branch")]
      public string? DefaultBranch { get; init; }

      [JsonPropertyName("description")]
      public string? Description { get; init; }

      [JsonPropertyName("full_name")]
      public string? FullName { get; init; }

      [JsonPropertyName("html_url")]
      public string? HtmlUrl { get; init; }

      [JsonPropertyName("owner")]
      public GitHubOwnerResponse? Owner { get; init; }

      [JsonPropertyName("private")]
      public bool Private { get; init; }

      [JsonPropertyName("visibility")]
      public string? Visibility { get; init; }

      #endregion
   }

   #endregion
}
