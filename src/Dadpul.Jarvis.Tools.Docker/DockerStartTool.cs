// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Docker.Contracts;
using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
public class DockerStartTool : DockerHandling, ITool
{
   #region Constructors and Destructors

   [ImportingConstructor]
   public DockerStartTool(IDockerController dockerController)
      : base(dockerController)
   {
   }

   #endregion

   #region ITool Members

   public string Description =>
      "Starts one exact Docker container. Use docker_list_containers first " + "unless the exact host and container name are already available from "
                                                                             + "recent tool output. When exactly one listed container clearly matches "
                                                                             + "the user's request, call this tool in the same turn. Only start a "
                                                                             + "container when the user explicitly requests it.";

   public string Name { get; } = "docker_start";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["node"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] = "The exact Docker host returned by docker_list_containers.",
               ["enum"] = CreateNodeArray()
            },
            ["containerName"] =
               new JsonObject { ["type"] = "string", ["description"] = "The exact container name returned by docker_list_containers." }
         },
         ["required"] = new JsonArray("node", "containerName"),
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.3";

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "node", out var node))
      {
         return ToolResult.Failed("The 'node' argument is required.");
      }

      if (!TryGetString(arguments, "containerName", out var containerName))
      {
         return ToolResult.Failed("The 'containerName' argument is required.");
      }

      try
      {
         var result = await dockerController.StartContainerAsync(node, containerName, cancellationToken);

         return ToolResult.Successful(JsonSerializer.Serialize(result));
      }
      catch (DockerControllerException exception)
      {
         return ToolResult.Failed(exception.Message);
      }
   }

   #endregion
}