// Made by Dadpul

namespace Dadpul.Jarvis.Console.Tools.Lights;

internal sealed class VirtualLight
{
   #region Public Properties

   public bool IsOn { get; private set; } = true;

   #endregion

   #region Public Methods and Operators

   public void TurnOff()
   {
      IsOn = false;
   }

   public void TurnOn()
   {
      IsOn = true;
   }

   #endregion
}