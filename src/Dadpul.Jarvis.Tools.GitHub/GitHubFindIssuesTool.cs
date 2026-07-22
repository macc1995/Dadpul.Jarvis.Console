// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class GitHubFindIssuesTool : ITool
{
   #region Constants and Fields

   private static readonly HashSet<string> SupportedOrders =
      new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

   private static readonly HashSet<string> SupportedSorts =
      new(StringComparer.OrdinalIgnoreCase) { "best-match", "created", "updated", "comments" };

   private static readonly HashSet<string> SupportedStates =
      new(StringComparer.OrdinalIgnoreCase) { "open", "closed", "all" };

   private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

   private readonly IGitHubIssueService issueService;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubFindIssuesTool(IGitHubIssueService issueService)
   {
      this.issueService = issueService;
   }

   #endregion

   #region ITool Members

   public string Description =>
      "Searches and lists GitHub issues in an allowlisted repository. "
      + "Use this for backlog queries, finding issues by title or body text, or filtering issues by state, labels, or assignee. "
      + "Omit repository to use the configured default. Pull requests are excluded. "
      + "This tool returns compact summaries and does not retrieve complete issue bodies or comments.";

   public string Name => "github_find_issues";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["repository"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] = "Optional repository in owner/name format. Omit it to use the configured default."
            },
            ["query"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] = "Optional text to search for in issue titles and bodies."
            },
            ["state"] = new JsonObject
            {
               ["type"] = "string",
               ["enum"] = new JsonArray { "open", "closed", "all" },
               ["default"] = "open"
            },
            ["labels"] = new JsonObject
            {
               ["type"] = "array",
               ["items"] = new JsonObject { ["type"] = "string" },
               ["description"] = "Optional labels that every returned issue must have."
            },
            ["assignee"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] = "Optional GitHub username assigned to the issue."
            },
            ["limit"] = new JsonObject
            {
               ["type"] = "integer",
               ["minimum"] = 1,
               ["maximum"] = 30,
               ["default"] = 10
            },
            ["sort"] = new JsonObject
            {
               ["type"] = "string",
               ["enum"] = new JsonArray { "best-match", "created", "updated", "comments" },
               ["default"] = "updated"
            },
            ["order"] = new JsonObject
            {
               ["type"] = "string",
               ["enum"] = new JsonArray { "asc", "desc" },
               ["default"] = "desc"
            }
         },
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.0";

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryReadOptionalString(arguments, "repository", out var repository, out var error)
          || !TryReadOptionalString(arguments, "query", out var query, out error)
          || !TryReadOptionalString(arguments, "assignee", out var assignee, out error))
      {
         return ToolResult.Failed(error!);
      }

      if (!TryReadEnum(arguments, "state", "open", SupportedStates, out var state, out error)
          || !TryReadEnum(arguments, "sort", "updated", SupportedSorts, out var sort, out error)
          || !TryReadEnum(arguments, "order", "desc", SupportedOrders, out var order, out error))
      {
         return ToolResult.Failed(error!);
      }

      if (!TryReadLimit(arguments, out var limit, out error)
          || !TryReadLabels(arguments, out var labels, out error))
      {
         return ToolResult.Failed(error!);
      }

      try
      {
         var result = await issueService.FindIssuesAsync(
            new GitHubIssueSearchRequest(repository, query, state, labels, assignee, limit, sort, order),
            cancellationToken);

         return ToolResult.Successful(JsonSerializer.Serialize(result, JsonOptions));
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch (GitHubClientException exception)
      {
         return ToolResult.Failed(exception.Message);
      }
      catch (HttpRequestException exception)
      {
         return ToolResult.Failed($"GitHub request failed: {exception.Message}");
      }
      catch (TaskCanceledException)
      {
         return ToolResult.Failed("GitHub request timed out.");
      }
      catch (Exception exception)
      {
         Console.Error.WriteLine($"[github_find_issues failed]{Environment.NewLine}{exception}");

         return ToolResult.Failed($"GitHub issue search failed with {exception.GetType().Name}: {exception.Message}");
      }
   }

   #endregion

   #region Methods

   private static bool TryReadEnum(
      JsonObject arguments,
      string propertyName,
      string defaultValue,
      IReadOnlySet<string> supportedValues,
      out string value,
      out string? error)
   {
      value = defaultValue;
      error = null;

      if (!arguments.ContainsKey(propertyName))
      {
         return true;
      }

      if (arguments[propertyName] is not JsonValue jsonValue
          || !jsonValue.TryGetValue(out string? parsedValue)
          || string.IsNullOrWhiteSpace(parsedValue))
      {
         error = $"The argument '{propertyName}' must be a non-empty string.";
         return false;
      }

      parsedValue = parsedValue.Trim().ToLowerInvariant();

      if (!supportedValues.Contains(parsedValue))
      {
         error = $"Unsupported {propertyName} '{parsedValue}'. Supported values: {string.Join(", ", supportedValues)}.";
         return false;
      }

      value = parsedValue;
      return true;
   }

   private static bool TryReadLabels(
      JsonObject arguments,
      out IReadOnlyList<string> labels,
      out string? error)
   {
      labels = [];
      error = null;

      if (!arguments.ContainsKey("labels"))
      {
         return true;
      }

      if (arguments["labels"] is not JsonArray labelArray)
      {
         error = "The argument 'labels' must be an array of strings.";
         return false;
      }

      var parsedLabels = new List<string>();

      foreach (var item in labelArray)
      {
         if (item is not JsonValue jsonValue
             || !jsonValue.TryGetValue(out string? label)
             || string.IsNullOrWhiteSpace(label))
         {
            error = "Every labels entry must be a non-empty string.";
            return false;
         }

         parsedLabels.Add(label.Trim());
      }

      labels = parsedLabels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
      return true;
   }

   private static bool TryReadLimit(JsonObject arguments, out int limit, out string? error)
   {
      limit = 10;
      error = null;

      if (!arguments.ContainsKey("limit"))
      {
         return true;
      }

      if (arguments["limit"] is not JsonValue jsonValue
          || !jsonValue.TryGetValue(out limit)
          || limit is < 1 or > 30)
      {
         error = "The argument 'limit' must be an integer between 1 and 30.";
         return false;
      }

      return true;
   }

   private static bool TryReadOptionalString(
      JsonObject arguments,
      string propertyName,
      out string? value,
      out string? error)
   {
      value = null;
      error = null;

      if (!arguments.ContainsKey(propertyName))
      {
         return true;
      }

      if (arguments[propertyName] is not JsonValue jsonValue
          || !jsonValue.TryGetValue(out value)
          || string.IsNullOrWhiteSpace(value))
      {
         error = $"The argument '{propertyName}' must be a non-empty string.";
         return false;
      }

      value = value.Trim();
      return true;
   }

   #endregion
}
