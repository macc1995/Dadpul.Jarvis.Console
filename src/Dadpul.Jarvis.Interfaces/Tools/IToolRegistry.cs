// Made by Dadpul

namespace Dadpul.Jarvis.Interfaces.Tools;

public interface IToolRegistry
{
   #region Public Properties

   IReadOnlyCollection<ITool> Tools { get; }

   #endregion

   #region Public Methods and Operators

   void Register(ITool tool);

   bool TryGet(string name, out ITool? tool);

   #endregion
}