using System.Text.Json.Nodes;

namespace Dadpul.Jarvis.Console.Tools.Lights;

internal sealed class TurnOffLightTool : ITool
{
    private readonly VirtualLight light;

    public TurnOffLightTool(VirtualLight light)
    {
        this.light = light;
    }

    public string Name => "light_turn_off";

    public string Description =>
        "Turns off the virtual room light. Use this when the user asks " +
        "to turn off, disable, darken, or otherwise stop the light.";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    public Task<ToolResult> ExecuteAsync(
        JsonObject arguments,
        CancellationToken cancellationToken)
    {
        light.TurnOff();

        return Task.FromResult(
            ToolResult.Successful(
                "The virtual light is now off."));
    }
}