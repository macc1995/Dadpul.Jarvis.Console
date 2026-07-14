// Bonjour

namespace Dadpul.Jarvis.Tools.WebSearch;

public interface IWebSearchService
{
   #region Public Methods and Operators

   Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken);

   #endregion
}