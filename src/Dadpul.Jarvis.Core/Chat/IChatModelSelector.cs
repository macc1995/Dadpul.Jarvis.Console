// Made by Dadpul

namespace Dadpul.Jarvis.Core.Chat
{
   public interface IChatModelSelector
   {
      #region Public Methods and Operators

      Task<IChatModel> SelectAsync(CancellationToken cancellationToken);

      #endregion
   }
}