// Made by Dadpul

namespace Dadpul.Jarvis.Tools.WebSearch;

public interface IWebFetchService
{
   #region Public Methods and Operators

   Task<WebFetchResult> FetchAsync(Uri url, CancellationToken cancellationToken);

   #endregion
}