// Bonjour

namespace Dadpul.Jarvis.Tools.Memory;

public interface IMemoryStore
{
   Task StoreAsync(
       MemoryRecord memory,
       CancellationToken cancellationToken);

   Task<IReadOnlyList<MemoryRecord>> GetAllAsync(
       CancellationToken cancellationToken);

   Task<bool> DeleteAsync(
       Guid memoryId,
       CancellationToken cancellationToken);
}