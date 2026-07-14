// Bonjour

namespace Dadpul.Jarvis.Embeddings;

public interface IEmbeddingGenerator
{
   #region Public Methods and Operators

   Task<float[]> GenerateAsync(string input, EmbeddingInputType inputType, CancellationToken cancellationToken);

   #endregion
}

public enum EmbeddingInputType
{
   Query,

   Document
}