// Bonjour

namespace Dadpul.Jarvis.Tools.Memory;

public interface IMemoryStore
{
   #region Public Methods and Operators

   Task<bool> DeleteAsync(Guid memoryId, CancellationToken cancellationToken);

   Task<IReadOnlyList<MemoryRecord>> SearchAsync(string query, CancellationToken cancellationToken);

   Task<MemoryRecord> StoreAsync(string content, CancellationToken cancellationToken);

   #endregion
}