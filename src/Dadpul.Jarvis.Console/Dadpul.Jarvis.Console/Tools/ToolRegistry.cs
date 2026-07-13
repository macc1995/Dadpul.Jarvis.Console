namespace Dadpul.Jarvis.Console.Tools;

internal sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> tools =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ITool> Tools =>
        tools.Values;

    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (!tools.TryAdd(tool.Name, tool))
        {
            throw new InvalidOperationException(
                $"A tool named '{tool.Name}' is already registered.");
        }
    }

    public bool TryGet(
        string name,
        out ITool? tool)
    {
        return tools.TryGetValue(name, out tool);
    }
}