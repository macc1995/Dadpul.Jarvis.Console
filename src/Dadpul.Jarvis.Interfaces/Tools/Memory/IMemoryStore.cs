// Made by Dadpul

namespace Dadpul.Jarvis.Interfaces.Tools.Memory;

public interface IMemoryStore
{
   #region Public Methods and Operators

   Task<bool> DeleteAsync(Guid memoryId, CancellationToken cancellationToken);

   Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken);

   Task StoreAsync(MemoryRecord memory, CancellationToken cancellationToken);

   #endregion
}