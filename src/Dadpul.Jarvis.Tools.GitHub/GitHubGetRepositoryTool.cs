// Made by Dadpul

namespace Dadpul.Jarvis.Tools.GitHub;

using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class GitHubGetRepositoryTool : ITool
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

   private readonly IGitHubRepositoryService repositoryService;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public GitHubGetRepositoryTool(IGitHubRepositoryService repositoryService)
   {
      this.repositoryService = repositoryService;
   }

   #endregion

   #region ITool Members

   public string Description =>
      "Retrieves authoritative metadata for a configured GitHub repository. "
      + "Use this to verify that GitHub access is configured or when the user asks for repository-level information. "
      + "Omit repository to use the configured default. Only allowlisted repositories are accepted. "
      + "This tool does not search issues, read files, or modify GitHub.";

   public string Name => "github_get_repository";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["repository"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] =
                  "Optional repository in owner/name format. Omit it to use GitHub:DefaultRepository."
            }
         },
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.0";

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      string? repository = null;

      if (arguments.ContainsKey("repository"))
      {
         if (arguments["repository"] is not JsonValue jsonValue
             || !jsonValue.TryGetValue(out repository)
             || string.IsNullOrWhiteSpace(repository))
         {
            return ToolResult.Failed("The argument 'repository' must be a non-empty string in owner/name format.");
         }
      }

      try
      {
         var result = await repositoryService.GetRepositoryAsync(repository, cancellationToken);

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
         Console.Error.WriteLine($"[github_get_repository failed]{Environment.NewLine}{exception}");

         return ToolResult.Failed($"GitHub repository lookup failed with {exception.GetType().Name}: {exception.Message}");
      }
   }

   #endregion
}
