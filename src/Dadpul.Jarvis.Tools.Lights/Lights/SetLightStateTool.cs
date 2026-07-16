// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Lights.Lights;

using System.ComponentModel.Composition;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class SetLightStateTool : ITool
{
   #region Constants and Fields

   private readonly VirtualLight light;

   #endregion

   #region Constructors and Destructors

   public SetLightStateTool()
   {
      light = new VirtualLight();
   }

   #endregion

   #region ITool Members

   public string Description =>
      "Turns on or off the virtual room light. Use this when the user asks "
      + "to turn on or off, disable or enable, darken or lighten, or otherwise stop or start the light.";

   public string Name => "light_set_state";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["isOn"] = new JsonObject { ["type"] = "boolean", ["description"] = "Whether the light should be on." }
         },
         ["required"] = new JsonArray { "isOn" }
      };

   public string Version { get; } = "1.0.0";

   public Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (arguments["isOn"] is not JsonValue value || !value.TryGetValue(out bool isOn))
      {
         return Task.FromResult(ToolResult.Failed("The required boolean argument 'isOn' was missing or invalid."));
      }

      if (isOn)
      {
         light.TurnOn();
      }
      else
      {
         light.TurnOff();
      }

      var state = light.IsOn ? "on" : "off";

      return Task.FromResult(ToolResult.Successful($"The virtual light is now {state}."));
   }

   #endregion
}