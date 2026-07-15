// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.ComponentModel.Composition;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Docker.Contracts;
using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class DockerRestartContainerTool : DockerHandling, ITool
{
   #region Constructors and Destructors

   [ImportingConstructor]
   public DockerRestartContainerTool(IDockerController dockerController)
      : base(dockerController)
   {
   }

   #endregion

   #region ITool Members

   public string Name
   {
      get
      {
         return "docker_restart_container";
      }
   }

   public string Description
   {
      get
      {
         return "Restarts one exact Docker container. Before calling this tool, use "
                + "docker_list_containers unless an exact node and container ID are already "
                + "available in recent tool output. Never derive, invent or guess a container "
                + "ID. If several listed containers plausibly match the user's description, "
                + "ask the user which one they mean instead of restarting one.";
      }
   }

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
            ["containerName"] = new JsonObject
            {
               ["type"] = "string",
               ["description"] =
                  "The exact container name returned by docker_list_containers. " + "Copy it exactly; do not guess or rewrite it."
            }
         },
         ["required"] = new JsonArray("node", "containerName"),
         ["additionalProperties"] = false
      };

   public string Version { get; } = "1.0.6";

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
         var restartResult = await dockerController.RestartContainerAsync(node, containerName, cancellationToken);

         var result = new JsonObject
         {
            ["restarted"] = true, ["node"] = node, ["containerName"] = restartResult.ContainerName, ["message"] = restartResult.Message
         };

         return ToolResult.Successful(result.ToJsonString());
      }
      catch (DockerControllerException exception)
      {
         return ToolResult.Failed(exception.Message);
      }
   }

   #endregion
}