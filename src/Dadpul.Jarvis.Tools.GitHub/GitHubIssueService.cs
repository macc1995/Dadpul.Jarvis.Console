// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

[Export(typeof(IGitHubIssueService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed partial class GitHubIssueService : IGitHubIssueService
{
   #region Constants and Fields

   private readonly IGitHubApiClient apiClient;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubIssueService(IGitHubApiClient apiClient)
   {
      this.apiClient = apiClient;
   }

   #endregion

   #region IGitHubIssueService Members

   public async Task<GitHubIssueSearchResult> FindIssuesAsync(GitHubIssueSearchRequest request, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(request);

      var repository = apiClient.ResolveRepository(request.Repository);
      var searchQuery = BuildSearchQuery(repository, request);
      var relativePath = "search/issues?q=" + Uri.EscapeDataString(searchQuery) + $"&sort={Uri.EscapeDataString(request.Sort)}"
                         + $"&order={Uri.EscapeDataString(request.Order)}" + $"&per_page={request.Limit}";

      var response = await apiClient.GetAsync<GitHubIssueSearchResponse>(repository, relativePath, cancellationToken);

      var issues = response.Items.Where(item => item.PullRequest is null).Take(request.Limit).Select(MapIssue).ToArray();

      return new GitHubIssueSearchResult(repository, issues, response.TotalCount, response.IncompleteResults);
   }

   #endregion

   #region Methods

   private static string BuildSearchQuery(string repository, GitHubIssueSearchRequest request)
   {
      var qualifiers = new List<string> { $"repo:{repository}", "is:issue", "in:title,body" };

      if (!request.State.Equals("all", StringComparison.OrdinalIgnoreCase))
      {
         qualifiers.Add($"state:{request.State}");
      }

      qualifiers.AddRange(request.Labels.Select(label => $"label:\"{EscapeQualifier(label)}\""));

      if (!string.IsNullOrWhiteSpace(request.Assignee))
      {
         qualifiers.Add($"assignee:{EscapeQualifier(request.Assignee)}");
      }

      if (!string.IsNullOrWhiteSpace(request.Query))
      {
         qualifiers.Insert(0, NeutralizeQualifierSyntax(request.Query));
      }

      return string.Join(' ', qualifiers);
   }

   private static string EscapeQualifier(string value)
   {
      return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
   }

   private static GitHubIssueSummary MapIssue(GitHubIssueResponse source)
   {
      if ((source.Number <= 0) || string.IsNullOrWhiteSpace(source.Title) || string.IsNullOrWhiteSpace(source.State)
          || string.IsNullOrWhiteSpace(source.HtmlUrl))
      {
         throw new GitHubClientException("GitHub returned an incomplete issue search result.");
      }

      var summarySource = string.IsNullOrWhiteSpace(source.Body) ? source.Title : source.Body;

      var summary = WhitespaceRegex().Replace(summarySource, " ").Trim();

      if (summary.Length > 300)
      {
         summary = summary[..300] + "…";
      }

      return new GitHubIssueSummary(source.Number, source.Title, source.State,
         source.Labels.Select(label => label.Name).OfType<string>().Where(name => !string.IsNullOrWhiteSpace(name)).ToArray(), source.UpdatedAt,
         source.HtmlUrl, summary);
   }

   private static string NeutralizeQualifierSyntax(string value)
   {
      return WhitespaceRegex().Replace(value.Replace(':', ' '), " ").Trim();
   }

   [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
   private static partial Regex WhitespaceRegex();

   #endregion

   #region Nested Types

   private sealed class GitHubIssueLabelResponse
   {
      #region Public Properties

      [JsonPropertyName("name")] public string? Name { get; init; }

      #endregion
   }

   private sealed class GitHubIssueResponse
   {
      #region Public Properties

      [JsonPropertyName("body")] public string? Body { get; init; }

      [JsonPropertyName("html_url")] public string HtmlUrl { get; init; } = string.Empty;

      [JsonPropertyName("labels")] public List<GitHubIssueLabelResponse> Labels { get; init; } = [];

      [JsonPropertyName("number")] public int Number { get; init; }

      [JsonPropertyName("pull_request")] public object? PullRequest { get; init; }

      [JsonPropertyName("state")] public string State { get; init; } = string.Empty;

      [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;

      [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; init; }

      #endregion
   }

   private sealed class GitHubIssueSearchResponse
   {
      #region Public Properties

      [JsonPropertyName("incomplete_results")]
      public bool IncompleteResults { get; init; }

      [JsonPropertyName("items")] public List<GitHubIssueResponse> Items { get; init; } = [];

      [JsonPropertyName("total_count")] public int TotalCount { get; init; }

      #endregion
   }

   #endregion
}