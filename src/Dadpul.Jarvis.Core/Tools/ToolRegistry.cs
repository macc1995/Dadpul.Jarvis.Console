// Made by Dadpul

namespace Dadpul.Jarvis.Core.Tools;

using System.ComponentModel.Composition;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(IToolRegistry))]
public sealed class ToolRegistry : IToolRegistry
{
   #region Constants and Fields

   private readonly Dictionary<string, ITool> tools = new(StringComparer.OrdinalIgnoreCase);

   #endregion

   #region IToolRegistry Members

   public void Register(ITool tool)
   {
      ArgumentNullException.ThrowIfNull(tool);

      if (!tools.TryAdd(tool.Name, tool))
      {
         throw new InvalidOperationException($"A tool named '{tool.Name}' is already registered.");
      }
   }

   public bool TryGet(string name, out ITool? tool)
   {
      return tools.TryGetValue(name, out tool);
   }

   public IReadOnlyCollection<ITool> Tools => tools.Values;

   #endregion
}