// Bonjour

namespace Dadpul.Jarvis.Tools.Memory;

using System.ComponentModel.Composition;
using System.Text.Json;

[Export(typeof(IMemoryStore))]
internal sealed class JsonMemoryStore : IMemoryStore
{
   #region Constants and Fields

   private readonly SemaphoreSlim fileLock = new(1, 1);

   private readonly string filePath;

   private readonly JsonSerializerOptions serializerOptions;

   #endregion

   #region Constructors and Destructors

   public JsonMemoryStore()
   {
      var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

      filePath = Path.Combine(dataDirectory, "memories.json");

      serializerOptions = new JsonSerializerOptions { WriteIndented = true };
   }

   #endregion

   #region IMemoryStore Members

   public async Task<MemoryRecord> StoreAsync(string content, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(content))
      {
         throw new ArgumentException("Memory content cannot be empty.", nameof(content));
      }

      await fileLock.WaitAsync(cancellationToken);

      try
      {
         var memories = await LoadMemoriesAsync(cancellationToken);

         var memory = new MemoryRecord { Id = Guid.NewGuid(), Content = content.Trim(), CreatedAt = DateTimeOffset.UtcNow };

         memories.Add(memory);

         await SaveMemoriesAsync(memories, cancellationToken);

         return memory;
      }
      finally
      {
         fileLock.Release();
      }
   }

   public async Task<IReadOnlyList<MemoryRecord>> SearchAsync(string query, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(query))
      {
         return Array.Empty<MemoryRecord>();
      }

      await fileLock.WaitAsync(cancellationToken);

      try
      {
         var memories = await LoadMemoriesAsync(cancellationToken);

         return memories.Where(memory => memory.Content.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(memory => memory.UpdatedAt ?? memory.CreatedAt).ToList();
      }
      finally
      {
         fileLock.Release();
      }
   }

   public async Task<bool> DeleteAsync(Guid memoryId, CancellationToken cancellationToken)
   {
      await fileLock.WaitAsync(cancellationToken);

      try
      {
         var memories = await LoadMemoriesAsync(cancellationToken);

         var removedCount = memories.RemoveAll(memory => memory.Id == memoryId);

         if (removedCount == 0)
         {
            return false;
         }

         await SaveMemoriesAsync(memories, cancellationToken);

         return true;
      }
      finally
      {
         fileLock.Release();
      }
   }

   #endregion

   #region Methods

   private async Task<List<MemoryRecord>> LoadMemoriesAsync(CancellationToken cancellationToken)
   {
      if (!File.Exists(filePath))
      {
         return [];
      }

      await using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

      if (stream.Length == 0)
      {
         return [];
      }

      var memories = await JsonSerializer.DeserializeAsync<List<MemoryRecord>>(stream, serializerOptions, cancellationToken);

      return memories ?? [];
   }

   private async Task SaveMemoriesAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken)
   {
      var directoryPath = Path.GetDirectoryName(filePath);

      if (directoryPath is null)
      {
         throw new InvalidOperationException($"Could not determine the directory for '{filePath}'.");
      }

      Directory.CreateDirectory(directoryPath);

      var temporaryFilePath = filePath + ".tmp";

      await using (FileStream stream = new(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
      {
         await JsonSerializer.SerializeAsync(stream, memories, serializerOptions, cancellationToken);

         await stream.FlushAsync(cancellationToken);
      }

      File.Move(temporaryFilePath, filePath, overwrite: true);
   }

   #endregion
}