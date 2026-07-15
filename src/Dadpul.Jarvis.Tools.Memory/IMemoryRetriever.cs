// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Memory;

public interface IMemoryRetriever
{
   #region Public Methods and Operators

   Task<IReadOnlyList<MemoryMatch>> RetrieveAsync(string query, CancellationToken cancellationToken);

   #endregion
}