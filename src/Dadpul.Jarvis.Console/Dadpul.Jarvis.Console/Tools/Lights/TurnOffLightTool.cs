// Bonjour

namespace Dadpul.Jarvis.Console.Tools.Lights;

using System.Text.Json.Nodes;

internal sealed class TurnOffLightTool : ITool
{
   #region Constants and Fields

   private readonly VirtualLight light;

   #endregion

   #region Constructors and Destructors

   public TurnOffLightTool(VirtualLight light)
   {
      this.light = light;
   }

   #endregion

   #region ITool Members

   public string Name => "light_turn_off";

   public string Description =>
      "Turns off the virtual room light. Use this when the user asks " + "to turn off, disable, darken, or otherwise stop the light.";

   public JsonObject Parameters => new() { ["type"] = "object", ["properties"] = new JsonObject(), ["required"] = new JsonArray() };

   public Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      light.TurnOff();

      return Task.FromResult(ToolResult.Successful("The virtual light is now off."));
   }

   #endregion
}