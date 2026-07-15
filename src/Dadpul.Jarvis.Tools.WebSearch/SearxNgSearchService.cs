// Made by Dadpul

namespace Dadpul.Jarvis.Tools.WebSearch;

using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

[Export(typeof(IWebSearchService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SearxNgSearchService : IWebSearchService, IDisposable
{
   #region Constants and Fields

   private const string SearxNgUrlEnvironmentVariable = "JARVIS_SEARXNG_URL";

   private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

   private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

   private readonly HttpClient httpClient;

   #endregion

   #region Constructors and Destructors

   public SearxNgSearchService()
   {
      var baseUrl = "http://192.168.0.70:8080/";

      if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
      {
         throw new InvalidOperationException($"The value of {baseUrl} is not a valid absolute URL.");
      }

      if ((baseUri.Scheme != Uri.UriSchemeHttp) && (baseUri.Scheme != Uri.UriSchemeHttps))
      {
         throw new InvalidOperationException($"{baseUrl} must use HTTP or HTTPS.");
      }

      httpClient = new HttpClient { BaseAddress = EnsureTrailingSlash(baseUri), Timeout = TimeSpan.FromSeconds(20) };

      httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Dadpul-Jarvis/0.1");
   }

   #endregion

   #region IDisposable Members

   public void Dispose()
   {
      httpClient.Dispose();
   }

   #endregion

   #region IWebSearchService Members

   public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(request);

      if (string.IsNullOrWhiteSpace(request.Query))
      {
         throw new ArgumentException("The search query cannot be empty.", nameof(request));
      }

      var requestUri = BuildSearchUri(request);

      using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

      // Prevents SearXNG's noisy bot-detection error.
      // This instance is private and bound to localhost.
      requestMessage.Headers.TryAddWithoutValidation("X-Real-IP", "127.0.0.1");

      requestMessage.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");

      using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

      var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException($"SearXNG returned HTTP {(int)response.StatusCode} " + $"({response.ReasonPhrase}). "
                                                                                             + $"Response body: {Truncate(responseContent, 2_000)}");
      }

      SearxNgResponse? searchResponse;

      try
      {
         searchResponse = JsonSerializer.Deserialize<SearxNgResponse>(responseContent, JsonOptions);
      }
      catch (JsonException exception)
      {
         throw new JsonException($"SearXNG returned invalid or unexpected JSON. " + $"Response body: {Truncate(responseContent, 2_000)}", exception);
      }

      if (searchResponse?.Results is null)
      {
         throw new JsonException($"The SearXNG response did not contain a results array. " + $"Response body: {Truncate(responseContent, 2_000)}");
      }

      return searchResponse.Results.Select(MapResult).Where(result => result is not null).Cast<WebSearchResult>()
         .GroupBy(result => result.Url, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).Take(request.MaxResults).ToArray();
   }

   #endregion

   #region Methods

   private static string BuildSearchUri(WebSearchRequest request)
   {
      var parameters = new List<string>
      {
         $"q={Uri.EscapeDataString(request.Query.Trim())}", "format=json", $"language={Uri.EscapeDataString(request.Language)}"
      };

      if (!string.IsNullOrWhiteSpace(request.TimeRange))
      {
         parameters.Add($"time_range={Uri.EscapeDataString(request.TimeRange)}");
      }

      return "search?" + string.Join("&", parameters);
   }

   private static Uri EnsureTrailingSlash(Uri uri)
   {
      var value = uri.AbsoluteUri;

      return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(value + "/", UriKind.Absolute);
   }

   private static WebSearchResult? MapResult(SearxNgResult source)
   {
      if (string.IsNullOrWhiteSpace(source.Title) || string.IsNullOrWhiteSpace(source.Url))
      {
         return null;
      }

      if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri))
      {
         return null;
      }

      if ((uri.Scheme != Uri.UriSchemeHttp) && (uri.Scheme != Uri.UriSchemeHttps))
      {
         return null;
      }

      var engines = source.Engines?.Where(engine => !string.IsNullOrWhiteSpace(engine)).Select(engine => NormalizeText(engine, 100))
         .Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();

      return new WebSearchResult(NormalizeText(source.Title, 300), uri.AbsoluteUri, NormalizeText(source.Content, 1_200), engines,
         NormalizeOptionalText(source.PublishedDate, 100));
   }

   private static string? NormalizeOptionalText(string? value, int maximumLength)
   {
      var normalized = NormalizeText(value, maximumLength);

      return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
   }

   private static string NormalizeText(string? value, int maximumLength)
   {
      if (string.IsNullOrWhiteSpace(value))
      {
         return string.Empty;
      }

      var withoutTags = HtmlTagRegex.Replace(value, " ");
      var decoded = WebUtility.HtmlDecode(withoutTags);
      var normalized = WhitespaceRegex.Replace(decoded, " ").Trim();

      if (normalized.Length <= maximumLength)
      {
         return normalized;
      }

      return normalized[..maximumLength] + "…";
   }

   private static string Truncate(string? value, int maximumLength)
   {
      if (string.IsNullOrEmpty(value))
      {
         return "<empty>";
      }

      return value.Length <= maximumLength ? value : value[..maximumLength] + "…";
   }

   #endregion

   #region Nested Types

   private sealed class SearxNgResponse
   {
      #region Public Properties

      [JsonPropertyName("results")] public List<SearxNgResult>? Results { get; init; }

      #endregion
   }

   private sealed class SearxNgResult
   {
      #region Public Properties

      [JsonPropertyName("content")] public string? Content { get; init; }

      [JsonPropertyName("engines")] public List<string>? Engines { get; init; }

      [JsonPropertyName("publishedDate")] public string? PublishedDate { get; init; }

      [JsonPropertyName("title")] public string? Title { get; init; }

      [JsonPropertyName("url")] public string? Url { get; init; }

      #endregion
   }

   #endregion
}