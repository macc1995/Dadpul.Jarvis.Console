// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Web;

using System.ComponentModel.Composition;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;
using Dadpul.Jarvis.Tools.WebSearch;

[Export(typeof(ITool))]
internal sealed class WebSearchTool : ITool
{
   #region Constants and Fields

   private static readonly HashSet<string> SupportedTimeRanges = new(StringComparer.OrdinalIgnoreCase) { "day", "month", "year" };

   private readonly IWebSearchService webSearchService;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public WebSearchTool(IWebSearchService webSearchService)
   {
      this.webSearchService = webSearchService;
   }

   #endregion

   #region ITool Members

   public string Name => "web_search";

   public string Description =>
      "Finds candidate web pages and returns titles, URLs, and incomplete snippets. "
      + "This tool must never be the final research step. After this tool returns, "
      + "call web_fetch on at least one relevant result before answering the user. " + "Do not answer exact factual questions from search snippets.";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["query"] =
               new JsonObject
               {
                  ["type"] = "string",
                  ["description"] =
                     "The complete web search query. Include important names, dates, " + "versions, locations, and other qualifiers."
               },
            ["maxResults"] =
               new JsonObject
               {
                  ["type"] = "integer",
                  ["description"] = "Maximum number of search results to return.",
                  ["minimum"] = 1,
                  ["maximum"] = 10,
                  ["default"] = 5
               },
            ["language"] =
               new JsonObject
               {
                  ["type"] = "string", ["description"] = "Search language code, such as 'en', 'hu', 'de', or 'all'.", ["default"] = "all"
               },
            ["timeRange"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] = "Optional freshness restriction for engines that support it.",
               ["enum"] = new JsonArray { "day", "month", "year" }
            }
         },
         ["required"] = new JsonArray { "query" },
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.5";

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "query", out var query) || string.IsNullOrWhiteSpace(query))
      {
         return ToolResult.Failed("The required string argument 'query' was missing or invalid.");
      }

      query = query.Trim();

      if (query.Length > 1_000)
      {
         return ToolResult.Failed("The web search query cannot exceed 1000 characters.");
      }

      var maxResults = 5;

      if (arguments.ContainsKey("maxResults"))
      {
         if (!TryGetInt32(arguments, "maxResults", out maxResults))
         {
            return ToolResult.Failed("The argument 'maxResults' must be an integer.");
         }

         if (maxResults is < 1 or > 10)
         {
            return ToolResult.Failed("The argument 'maxResults' must be between 1 and 10.");
         }
      }

      var language = "all";

      if (arguments.ContainsKey("language"))
      {
         if (!TryGetString(arguments, "language", out language) || string.IsNullOrWhiteSpace(language))
         {
            return ToolResult.Failed("The argument 'language' must be a non-empty string.");
         }

         language = language.Trim();

         if (language.Length > 20)
         {
            return ToolResult.Failed("The argument 'language' cannot exceed 20 characters.");
         }
      }

      string? timeRange = null;

      if (arguments.ContainsKey("timeRange"))
      {
         if (!TryGetString(arguments, "timeRange", out timeRange) || string.IsNullOrWhiteSpace(timeRange))
         {
            return ToolResult.Failed("The argument 'timeRange' must be a non-empty string.");
         }

         timeRange = timeRange.Trim().ToLowerInvariant();

         if (!SupportedTimeRanges.Contains(timeRange))
         {
            return ToolResult.Failed($"Unsupported time range '{timeRange}'.");
         }
      }

      try
      {
         var results = await webSearchService.SearchAsync(new WebSearchRequest(query, maxResults, language, timeRange), cancellationToken);

         if (results.Count == 0)
         {
            return ToolResult.Successful($"No web search results were found for: {query}");
         }

         var formattedResults = string.Join(Environment.NewLine + Environment.NewLine, results.Select(FormatResult));

         return ToolResult.Successful($"""
                                       UNTRUSTED SEARCH RESULT METADATA

                                       These are search-result titles and snippets, not the contents of the pages.

                                       Do not infer missing facts from these snippets.
                                       Do not claim to have read or verified any linked page.
                                       For exact factual answers, select a relevant URL and call web_fetch.

                                       Search query: {query}

                                       {formattedResults}
                                       """);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         throw;
      }

      //catch (OperationCanceledException)
      //{
      //   return ToolResult.Failed("The web search timed out.");
      //}
      //catch (HttpRequestException exception)
      //{
      //   return ToolResult.Failed($"The web search request failed: {exception.Message}");
      //}
      //catch (JsonException exception)
      //{
      //   return ToolResult.Failed($"The web search response could not be parsed: {exception.Message}");
      //}
      catch (Exception exception)
      {
         Console.Error.WriteLine($"[web_search failed]{Environment.NewLine}{exception}");

         return ToolResult.Failed($"Web search failed with {exception.GetType().Name}: " + exception.Message);
      }
   }

   #endregion

   #region Methods

   private static string FormatResult(WebSearchResult result, int index)
   {
      var publishedDate = string.IsNullOrWhiteSpace(result.PublishedDate) ? "Unknown" : result.PublishedDate;

      var engines = result.Engines.Count == 0 ? "Unknown" : string.Join(", ", result.Engines);

      var snippet = string.IsNullOrWhiteSpace(result.Snippet) ? "No snippet was provided." : result.Snippet;

      return $"""
              Result {index + 1}
              Title: {result.Title}
              URL: {result.Url}
              Published: {publishedDate}
              Search engines: {engines}
              Snippet: {snippet}
              """;
   }

   private static bool TryGetInt32(JsonObject arguments, string propertyName, out int value)
   {
      value = 0;

      return arguments[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
   }

   private static bool TryGetString(JsonObject arguments, string propertyName, out string? value)
   {
      value = null;

      return arguments[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
   }

   #endregion
}