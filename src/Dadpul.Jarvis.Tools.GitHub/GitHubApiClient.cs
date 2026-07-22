// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

[Export(typeof(IGitHubApiClient))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class GitHubApiClient : IGitHubApiClient, IDisposable
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions JsonOptions =
      new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private readonly HashSet<string> allowedRepositories;

   private readonly IGitHubAuthenticationProvider authenticationProvider;

   private readonly HttpClient httpClient;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubApiClient(
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
      DefaultRepository = NormalizeRepository(options.DefaultRepository);
      allowedRepositories = options.AllowedRepositories
         .Select(NormalizeRepository)
         .ToHashSet(StringComparer.OrdinalIgnoreCase);

      if (!allowedRepositories.Contains(DefaultRepository))
      {
         throw new GitHubClientException(
            $"GitHub:DefaultRepository '{DefaultRepository}' must also be present in GitHub:AllowedRepositories.");
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

   #region IGitHubApiClient Members

   public string DefaultRepository { get; }

   public async Task<T> GetAsync<T>(
      string? repository,
      string relativePath,
      CancellationToken cancellationToken)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

      var selectedRepository = ResolveRepository(repository);
      using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
      authenticationProvider.Apply(request);

      using var response = await httpClient.SendAsync(
         request,
         HttpCompletionOption.ResponseHeadersRead,
         cancellationToken);

      await EnsureSuccessfulAsync(selectedRepository, response, cancellationToken);

      try
      {
         return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new GitHubClientException(
                   $"GitHub returned an empty response for repository '{selectedRepository}'.");
      }
      catch (JsonException exception)
      {
         throw new GitHubClientException(
            $"GitHub returned invalid JSON for repository '{selectedRepository}'.",
            exception);
      }
   }

   public string ResolveRepository(string? repository)
   {
      var selectedRepository = string.IsNullOrWhiteSpace(repository)
                                  ? DefaultRepository
                                  : NormalizeRepository(repository);

      if (!allowedRepositories.Contains(selectedRepository))
      {
         throw new GitHubClientException(
            $"Repository '{selectedRepository}' is not allowed. Configured repositories: "
            + string.Join(", ", allowedRepositories.OrderBy(value => value)) + ".");
      }

      return selectedRepository;
   }

   #endregion

   #region Methods

   private static Uri EnsureTrailingSlash(Uri uri)
   {
      var value = uri.AbsoluteUri;

      return value.EndsWith("/", StringComparison.Ordinal)
                ? uri
                : new Uri(value + "/", UriKind.Absolute);
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
      var segments = normalized.Split(
         '/',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

   #endregion
}
