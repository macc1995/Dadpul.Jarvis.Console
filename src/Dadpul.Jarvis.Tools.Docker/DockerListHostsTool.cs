// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.ComponentModel.Composition;
using System.Text.Json;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Docker.Contracts;
using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class DockerListHostsTool : ITool
{
   #region Constants and Fields

   private readonly IDockerController dockerController;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public DockerListHostsTool(IDockerController dockerController)
   {
      this.dockerController = dockerController;
   }

   #endregion

   #region ITool Members

   public string Name
   {
      get
      {
         return "docker_list_hosts";
      }
   }

   public string Description
   {
      get
      {
         return "Lists the Docker hosts known to JARVIS. "
                + "Use when the user asks which Docker machines or nodes exist, or when the user asks to .";
      }
   }

   public JsonObject Parameters
   {
      get
      {
         return new JsonObject { ["type"] = "object", ["properties"] = new JsonObject(), ["additionalProperties"] = false };
      }
   }

   public string Version { get; } = "1.0.2";

   public Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      var result = new JsonObject { ["hosts"] = JsonSerializer.SerializeToNode(dockerController.NodeNames.OrderBy(name => name)) };

      return Task.FromResult(ToolResult.Successful(result.ToJsonString()));
   }

   #endregion
}