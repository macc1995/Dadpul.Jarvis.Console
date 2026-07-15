// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Docker.Contracts;
using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class DockerListContainersTool : ITool
{
   #region Constants and Fields

   private readonly IDockerController dockerController;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public DockerListContainersTool(IDockerController dockerController)
   {
      this.dockerController = dockerController;
   }

   #endregion

   #region ITool Members

   public string Name
   {
      get
      {
         return "docker_list_containers";
      }
   }

   public string Description =>
      "Lists Docker containers from every configured Docker host. " + "Use this to identify the exact host and container name when the user "
                                                                    + "describes a service naturally. Listing containers does not complete a "
                                                                    + "request to restart, start, stop or otherwise modify a container. "
                                                                    + "When exactly one container clearly matches a requested operation, "
                                                                    + "immediately call the appropriate operation tool in the same turn. "
                                                                    + "Do not merely announce that you will perform the operation."
                                                                    + "Always check stopped containers as well unless the user explicitly states not to.";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["includeStopped"] = new JsonObject
            {
               ["type"] = "boolean",
               ["description"] =
                  "Whether stopped containers should also be included. should be set to true by default except if the user explicitly states not to consider stopped containers.",
               ["default"] = true
            }
         },
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.6";

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      var includeStopped = !TryGetBoolean(arguments, "includeStopped", out var suppliedValue) || suppliedValue;

      try
      {
         var searchResult = await dockerController.SearchContainersAsync(node: null, query: null, includeStopped, cancellationToken);

         var containers = searchResult.Containers.Select(match =>
         {
            var container = match.Container;

            return new
            {
               node = match.Node,
               name = container.Names.FirstOrDefault() ?? container.Id,
               image = container.Image,
               state = container.State,
               status = container.Status,
               canRestart = HasLabelValue(container.Labels, "jarvis.manage", "true"),
               displayName = GetLabel(container.Labels, "jarvis.display-name"),
               description = GetLabel(container.Labels, "jarvis.description"),
               aliases = GetLabel(container.Labels, "jarvis.aliases"),
               composeProject = GetLabel(container.Labels, "com.docker.compose.project"),
               composeService = GetLabel(container.Labels, "com.docker.compose.service")
            };
         }).ToArray();

         var result = new JsonObject
         {
            ["complete"] = searchResult.FailedNodes.Count == 0,
            ["containerCount"] = containers.Length,
            ["containers"] = JsonSerializer.SerializeToNode(containers),
            ["failedNodes"] = JsonSerializer.SerializeToNode(searchResult.FailedNodes)
         };

         return ToolResult.Successful(result.ToJsonString());
      }
      catch (DockerControllerException exception)
      {
         return ToolResult.Failed(exception.Message);
      }
   }

   #endregion

   #region Methods

   private static string? GetLabel(IReadOnlyDictionary<string, string> labels, string name)
   {
      return labels.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
   }

   private static bool HasLabelValue(IReadOnlyDictionary<string, string> labels, string name, string expectedValue)
   {
      return labels.TryGetValue(name, out var value) && string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
   }

   private static bool TryGetBoolean(JsonObject arguments, string name, out bool value)
   {
      value = false;

      return arguments[name] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
   }

   #endregion
}