// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Memory;

#region Interfaces

public interface IMemoryService
{
   #region Public Methods and Operators

   Task<ForgetMemoryResult> ForgetAsync(string query, CancellationToken cancellationToken);

   Task<MemoryRecord> RememberAsync(string content, CancellationToken cancellationToken);

   #endregion
}

#endregion