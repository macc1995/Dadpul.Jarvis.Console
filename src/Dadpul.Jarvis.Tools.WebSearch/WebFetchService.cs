// Bonjour

namespace Dadpul.Jarvis.Tools.WebSearch;

using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

[Export(typeof(IWebFetchService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class WebFetchService : IWebFetchService, IDisposable
{
   #region Constants and Fields

   private const int MaximumDownloadBytes = 2 * 1024 * 1024;

   private const int MaximumExtractedCharacters = 24_000;

   private const int MaximumRedirects = 5;

   private const int MaximumStructuredDataCharacters = 12_000;

   private const int MaximumUrlLength = 4_096;

   private static readonly HashSet<string> BlockElements = new(StringComparer.OrdinalIgnoreCase)
   {
      "ADDRESS",
      "ARTICLE",
      "BLOCKQUOTE",
      "DD",
      "DIV",
      "DL",
      "DT",
      "FIELDSET",
      "FIGCAPTION",
      "FIGURE",
      "H1",
      "H2",
      "H3",
      "H4",
      "H5",
      "H6",
      "HR",
      "LI",
      "MAIN",
      "OL",
      "P",
      "PRE",
      "SECTION",
      "TABLE",
      "TBODY",
      "TD",
      "TFOOT",
      "TH",
      "THEAD",
      "TR",
      "UL"
   };

   private static readonly Regex RepeatedLineBreakRegex = new(@"(?:\r?\n\s*){3,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

   private static readonly Regex WhitespaceRegex = new(@"[^\S\r\n]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

   private readonly HttpClient httpClient;

   #endregion

   #region Constructors and Destructors

   public WebFetchService()
   {
      var handler = new SocketsHttpHandler
      {
         AllowAutoRedirect = false,
         AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
         ConnectTimeout = TimeSpan.FromSeconds(10),
         PooledConnectionLifetime = TimeSpan.FromMinutes(5),
         UseCookies = false,

         // A configured system proxy could otherwise bypass our DNS and
         // address validation and access resources reachable by the proxy.
         UseProxy = false
      };

      handler.ConnectCallback = ConnectToPublicEndpointAsync;

      httpClient = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };

      httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Dadpul-Jarvis/0.1");

      httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

      httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml", 0.9));

      httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.8));

      httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.8");
   }

   #endregion

   #region IDisposable Members

   public void Dispose()
   {
      httpClient.Dispose();
   }

   #endregion

   #region IWebFetchService Members

   public async Task<WebFetchResult> FetchAsync(Uri url, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(url);

      var requestedUri = ValidateAndNormalizeUri(url);
      var currentUri = requestedUri;

      for (var redirectCount = 0; redirectCount <= MaximumRedirects; redirectCount++)
      {
         using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);

         using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

         if (IsRedirect(response.StatusCode))
         {
            if (redirectCount >= MaximumRedirects)
            {
               throw new HttpRequestException($"The page exceeded the maximum of " + $"{MaximumRedirects} redirects.");
            }

            var location = response.Headers.Location;

            if (location is null)
            {
               throw new HttpRequestException($"The server returned HTTP " + $"{(int)response.StatusCode} without a Location header.");
            }

            var redirectUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);

            currentUri = ValidateAndNormalizeUri(redirectUri);

            continue;
         }

         if (!response.IsSuccessStatusCode)
         {
            throw new HttpRequestException($"The server returned HTTP " + $"{(int)response.StatusCode} "
                                                                        + $"({response.ReasonPhrase}) for '{currentUri}'.");
         }

         var mediaType = response.Content.Headers.ContentType?.MediaType?.Trim().ToLowerInvariant();

         if (!IsSupportedContentType(mediaType))
         {
            var displayedType = string.IsNullOrWhiteSpace(mediaType) ? "<missing>" : mediaType;

            throw new NotSupportedException($"The page returned unsupported content type " + $"'{displayedType}'. Web fetch currently supports HTML, "
                                                                                           + $"XHTML, plain text, and JSON.");
         }

         var download = await ReadLimitedAsync(response.Content, MaximumDownloadBytes, cancellationToken);

         var decodedContent = DecodeContent(download.Content, response.Content.Headers.ContentType?.CharSet);

         string? title = null;
         string extractedContent;

         if (IsHtml(mediaType, decodedContent))
         {
            extractedContent = ExtractReadableHtml(decodedContent, out title);
         }
         else
         {
            extractedContent = NormalizePlainText(decodedContent);
         }

         if (string.IsNullOrWhiteSpace(extractedContent))
         {
            throw new InvalidOperationException(BuildEmptyExtractionMessage(currentUri, decodedContent, title, download.Content.Length));
         }

         var textTruncated = false;

         if (extractedContent.Length > MaximumExtractedCharacters)
         {
            extractedContent = extractedContent[..MaximumExtractedCharacters].TrimEnd() + Environment.NewLine + "[EXTRACTED CONTENT TRUNCATED]";

            textTruncated = true;
         }

         return new WebFetchResult(requestedUri.AbsoluteUri, currentUri.AbsoluteUri, title, mediaType ?? "unknown", extractedContent,
            download.Truncated || textTruncated);
      }

      throw new InvalidOperationException("The redirect loop terminated unexpectedly.");
   }

   #endregion

   #region Methods

   private static void AppendLimitedStructuredData(StringBuilder builder, string value)
   {
      if (builder.Length >= MaximumStructuredDataCharacters)
      {
         return;
      }

      var remaining = MaximumStructuredDataCharacters - builder.Length;

      if (builder.Length > 0)
      {
         const string separator = "\n\n--- NEXT STRUCTURED DATA BLOCK ---\n\n";

         if (separator.Length >= remaining)
         {
            return;
         }

         builder.Append(separator);
         remaining -= separator.Length;
      }

      if (value.Length <= remaining)
      {
         builder.Append(value);
         return;
      }

      builder.Append(value.AsSpan(0, remaining));
      builder.Append(Environment.NewLine + "[STRUCTURED DATA TRUNCATED]");
   }

   private static void AppendLineBreak(StringBuilder builder)
   {
      if (builder.Length == 0)
      {
         return;
      }

      while ((builder.Length > 0) && (builder[^1] == ' '))
      {
         builder.Length--;
      }

      if (builder[^1] != '\n')
      {
         builder.AppendLine();
      }
   }

   private static void AppendReadableText(INode node, StringBuilder builder)
   {
      if (node is IText textNode)
      {
         AppendText(builder, textNode.Data);
         return;
      }

      if (node is not IElement element)
      {
         foreach (var child in node.ChildNodes)
         {
            AppendReadableText(child, builder);
         }

         return;
      }

      if (element.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase))
      {
         AppendLineBreak(builder);
         return;
      }

      var isBlock = BlockElements.Contains(element.TagName);

      if (isBlock)
      {
         AppendLineBreak(builder);
      }

      foreach (var child in element.ChildNodes)
      {
         AppendReadableText(child, builder);
      }

      if (isBlock)
      {
         AppendLineBreak(builder);
      }
   }

   private static void AppendText(StringBuilder builder, string? text)
   {
      if (string.IsNullOrWhiteSpace(text))
      {
         return;
      }

      var normalized = WhitespaceRegex.Replace(text, " ").Trim();

      if (normalized.Length == 0)
      {
         return;
      }

      if ((builder.Length > 0) && !char.IsWhiteSpace(builder[^1]) && !StartsWithClosingPunctuation(normalized))
      {
         builder.Append(' ');
      }

      builder.Append(normalized);
   }

   private static string BuildEmptyExtractionMessage(Uri url, string html, string? title, int downloadedBytes)
   {
      var normalizedHtml = html.ToLowerInvariant();

      string likelyReason;

      if (ContainsAny(normalizedHtml, "verify you are human", "robot check", "captcha", "access denied", "cf-chl-", "challenge-platform"))
      {
         likelyReason = "The website appears to have returned an anti-bot or " + "human-verification page.";
      }
      else if (ContainsAny(normalizedHtml, "enable javascript", "javascript is disabled", "requires javascript", "please turn on javascript"))
      {
         likelyReason = "The website appears to require JavaScript rendering.";
      }
      else
      {
         likelyReason = "The response may be an empty application shell or may contain "
                        + "content in a format the current extractor does not support.";
      }

      var displayedTitle = string.IsNullOrWhiteSpace(title) ? "<none>" : title;

      return $"""
              No useful readable content could be extracted from '{url}'.

              Downloaded bytes: {downloadedBytes:N0}
              Page title: {displayedTitle}
              Likely reason: {likelyReason}

              Try fetching another search result from a less restrictive or
              server-rendered source.
              """;
   }

   private static async ValueTask<Stream> ConnectToPublicEndpointAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
   {
      var host = context.DnsEndPoint.Host;
      var port = context.DnsEndPoint.Port;

      IPAddress[] resolvedAddresses;

      if (IPAddress.TryParse(host, out var literalAddress))
      {
         resolvedAddresses = new[] { literalAddress };
      }
      else
      {
         resolvedAddresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
      }

      var publicAddresses = resolvedAddresses.Where(address => !IsBlockedAddress(address)).Distinct().ToArray();

      if (publicAddresses.Length == 0)
      {
         throw new HttpRequestException($"Access to '{host}' was blocked because it resolved only "
                                        + "to local, private, reserved, or otherwise unsafe addresses.");
      }

      Exception? lastException = null;

      foreach (var address in publicAddresses)
      {
         var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

         try
         {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
         }
         catch (OperationCanceledException)
         {
            socket.Dispose();
            throw;
         }
         catch (SocketException exception)
         {
            socket.Dispose();
            lastException = exception;
         }
      }

      throw new HttpRequestException($"Could not connect to any public address resolved for '{host}'.", lastException);
   }

   private static bool ContainsAny(string value, params string[] candidates)
   {
      return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
   }

   private static string DecodeContent(byte[] content, string? declaredCharacterSet)
   {
      var encoding = Encoding.UTF8;

      if (!string.IsNullOrWhiteSpace(declaredCharacterSet))
      {
         var normalizedCharacterSet = declaredCharacterSet.Trim().Trim('"', '\'');

         try
         {
            encoding = Encoding.GetEncoding(normalizedCharacterSet, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
         }
         catch (ArgumentException)
         {
            // Invalid or unsupported charset: UTF-8 remains the fallback.
         }
      }

      using var stream = new MemoryStream(content);
      using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);

      return reader.ReadToEnd();
   }

   private static string ExtractElementText(IElement element)
   {
      var builder = new StringBuilder();

      AppendReadableText(element, builder);

      var structuredText = NormalizeExtractedText(builder.ToString());

      if (!string.IsNullOrWhiteSpace(structuredText))
      {
         return structuredText;
      }

      // Simpler fallback in case the custom traversal encounters
      // an unexpected DOM structure.
      return NormalizeExtractedText(element.TextContent);
   }

   private static string ExtractJsonLd(IDocument document)
   {
      var builder = new StringBuilder();

      foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
      {
         var rawJson = script.TextContent.Trim();

         if (string.IsNullOrWhiteSpace(rawJson))
         {
            continue;
         }

         try
         {
            using var jsonDocument = JsonDocument.Parse(rawJson);

            var normalizedJson = JsonSerializer.Serialize(jsonDocument.RootElement, new JsonSerializerOptions { WriteIndented = true });

            AppendLimitedStructuredData(builder, normalizedJson);

            if (builder.Length >= MaximumStructuredDataCharacters)
            {
               break;
            }
         }
         catch (JsonException)
         {
            // Some sites put malformed or non-standard content into
            // application/ld+json scripts. Ignore those individual blocks.
         }
      }

      return builder.ToString().Trim();
   }

   private static string ExtractPageMetadata(IDocument document)
   {
      var description = FirstNonEmpty(NormalizeSingleLine(document.QuerySelector("meta[name='description']")?.GetAttribute("content")),
         NormalizeSingleLine(document.QuerySelector("meta[property='og:description']")?.GetAttribute("content")),
         NormalizeSingleLine(document.QuerySelector("meta[name='twitter:description']")?.GetAttribute("content")));

      var siteName = NormalizeSingleLine(document.QuerySelector("meta[property='og:site_name']")?.GetAttribute("content"));

      var canonicalUrl = document.QuerySelector("link[rel='canonical']")?.GetAttribute("href");

      var parts = new List<string>();

      if (!string.IsNullOrWhiteSpace(description))
      {
         parts.Add($"Description: {description}");
      }

      if (!string.IsNullOrWhiteSpace(siteName))
      {
         parts.Add($"Site: {siteName}");
      }

      if (!string.IsNullOrWhiteSpace(canonicalUrl))
      {
         parts.Add($"Canonical URL: {canonicalUrl}");
      }

      return string.Join(Environment.NewLine, parts);
   }

   private static string ExtractReadableHtml(string html, out string? title)
   {
      var parser = new HtmlParser();
      var document = parser.ParseDocument(html);

      title = FirstNonEmpty(NormalizeSingleLine(document.Title),
         NormalizeSingleLine(document.QuerySelector("meta[property='og:title']")?.GetAttribute("content")),
         NormalizeSingleLine(document.QuerySelector("meta[name='twitter:title']")?.GetAttribute("content")),
         NormalizeSingleLine(document.QuerySelector("h1")?.TextContent));

      // This must happen before scripts are removed.
      var structuredData = ExtractJsonLd(document);

      var metadata = ExtractPageMetadata(document);

      const string removableSelector = "script, style, noscript, template, svg, canvas, iframe, "
                                       + "object, embed, form, nav, header, footer, aside, " + "[hidden], [aria-hidden='true']";

      foreach (var element in document.QuerySelectorAll(removableSelector).ToArray())
      {
         element.Remove();
      }

      var candidateRoots = new[]
      {
         document.QuerySelector("article"), document.QuerySelector("main"), document.QuerySelector("[role='main']"),
         document.QuerySelector("#content"), document.QuerySelector(".content"), document.Body, document.DocumentElement
      };

      var bestVisibleText = string.Empty;

      foreach (var candidateRoot in candidateRoots)
      {
         if (candidateRoot is null)
         {
            continue;
         }

         var candidateText = ExtractElementText(candidateRoot);

         if (candidateText.Length > bestVisibleText.Length)
         {
            bestVisibleText = candidateText;
         }
      }

      var parts = new List<string>();

      if (!string.IsNullOrWhiteSpace(metadata))
      {
         parts.Add($"PAGE METADATA{Environment.NewLine}{metadata}");
      }

      if (!string.IsNullOrWhiteSpace(bestVisibleText))
      {
         parts.Add($"VISIBLE PAGE TEXT{Environment.NewLine}{bestVisibleText}");
      }

      if (!string.IsNullOrWhiteSpace(structuredData))
      {
         parts.Add($"STRUCTURED PAGE DATA{Environment.NewLine}{structuredData}");
      }

      return NormalizeExtractedText(string.Join(Environment.NewLine + Environment.NewLine, parts));
   }

   private static string? FirstNonEmpty(params string?[] values)
   {
      return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
   }

   private static bool IsBlockedAddress(IPAddress address)
   {
      if (address.IsIPv4MappedToIPv6)
      {
         address = address.MapToIPv4();
      }

      if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.None)
          || address.Equals(IPAddress.IPv6None))
      {
         return true;
      }

      return address.AddressFamily switch
      {
         AddressFamily.InterNetwork => IsBlockedIpv4Address(address),

         AddressFamily.InterNetworkV6 => IsBlockedIpv6Address(address),

         _ => true
      };
   }

   private static bool IsBlockedHostName(string host)
   {
      return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
                                                                          || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
                                                                          || host.Equals("home.arpa", StringComparison.OrdinalIgnoreCase)
                                                                          || host.EndsWith(".home.arpa", StringComparison.OrdinalIgnoreCase);
   }

   private static bool IsBlockedIpv4Address(IPAddress address)
   {
      var bytes = address.GetAddressBytes();

      // 0.0.0.0/8
      if (bytes[0] == 0)
      {
         return true;
      }

      // 10.0.0.0/8
      if (bytes[0] == 10)
      {
         return true;
      }

      // 100.64.0.0/10 - carrier-grade NAT
      if ((bytes[0] == 100) && bytes[1] is >= 64 and <= 127)
      {
         return true;
      }

      // 127.0.0.0/8
      if (bytes[0] == 127)
      {
         return true;
      }

      // 169.254.0.0/16 - link local, including cloud metadata
      if ((bytes[0] == 169) && (bytes[1] == 254))
      {
         return true;
      }

      // 172.16.0.0/12
      if ((bytes[0] == 172) && bytes[1] is >= 16 and <= 31)
      {
         return true;
      }

      // 192.0.0.0/24
      if ((bytes[0] == 192) && (bytes[1] == 0) && (bytes[2] == 0))
      {
         return true;
      }

      // 192.0.2.0/24 - documentation
      if ((bytes[0] == 192) && (bytes[1] == 0) && (bytes[2] == 2))
      {
         return true;
      }

      // 192.88.99.0/24 - deprecated relay range
      if ((bytes[0] == 192) && (bytes[1] == 88) && (bytes[2] == 99))
      {
         return true;
      }

      // 192.168.0.0/16
      if ((bytes[0] == 192) && (bytes[1] == 168))
      {
         return true;
      }

      // 198.18.0.0/15 - benchmarking
      if ((bytes[0] == 198) && bytes[1] is 18 or 19)
      {
         return true;
      }

      // 198.51.100.0/24 - documentation
      if ((bytes[0] == 198) && (bytes[1] == 51) && (bytes[2] == 100))
      {
         return true;
      }

      // 203.0.113.0/24 - documentation
      if ((bytes[0] == 203) && (bytes[1] == 0) && (bytes[2] == 113))
      {
         return true;
      }

      // 224.0.0.0/4 multicast and 240.0.0.0/4 reserved
      return bytes[0] >= 224;
   }

   private static bool IsBlockedIpv6Address(IPAddress address)
   {
      if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
      {
         return true;
      }

      var bytes = address.GetAddressBytes();

      // Permit only the IPv6 global-unicast 2000::/3 range.
      if ((bytes[0] & 0xE0) != 0x20)
      {
         return true;
      }

      // 2001:db8::/32 - documentation
      if ((bytes[0] == 0x20) && (bytes[1] == 0x01) && (bytes[2] == 0x0D) && (bytes[3] == 0xB8))
      {
         return true;
      }

      return false;
   }

   private static bool IsHtml(string? mediaType, string decodedContent)
   {
      if (mediaType is "text/html" or "application/xhtml+xml")
      {
         return true;
      }

      if (!string.IsNullOrWhiteSpace(mediaType))
      {
         return false;
      }

      var trimmed = decodedContent.TrimStart();

      return trimmed.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)
             || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
   }

   private static bool IsRedirect(HttpStatusCode statusCode)
   {
      return statusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect
         or HttpStatusCode.PermanentRedirect;
   }

   private static bool IsSupportedContentType(string? mediaType)
   {
      return string.IsNullOrWhiteSpace(mediaType) || mediaType is "text/html" or "application/xhtml+xml" or "text/plain" or "application/json"
                                                  || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
   }

   private static string NormalizeExtractedText(string value)
   {
      if (string.IsNullOrWhiteSpace(value))
      {
         return string.Empty;
      }

      var normalizedLines = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n')
         .Select(line => WhitespaceRegex.Replace(line, " ").Trim()).Where(line => line.Length > 0);

      var normalized = string.Join(Environment.NewLine, normalizedLines);

      return RepeatedLineBreakRegex.Replace(normalized, Environment.NewLine + Environment.NewLine).Trim();
   }

   private static string NormalizePlainText(string value)
   {
      return NormalizeExtractedText(value);
   }

   private static string? NormalizeSingleLine(string? value)
   {
      if (string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return WhitespaceRegex.Replace(value, " ").Trim();
   }

   private static async Task<LimitedDownload> ReadLimitedAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
   {
      var declaredLength = content.Headers.ContentLength;

      if (declaredLength > maximumBytes)
      {
         throw new InvalidOperationException($"The page declares a size of {declaredLength:N0} bytes, "
                                             + $"which exceeds the {maximumBytes:N0}-byte limit.");
      }

      await using var input = await content.ReadAsStreamAsync(cancellationToken);

      var buffer = new byte[maximumBytes + 1];
      var totalRead = 0;

      while (totalRead < buffer.Length)
      {
         var read = await input.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);

         if (read == 0)
         {
            break;
         }

         totalRead += read;
      }

      var truncated = totalRead > maximumBytes;
      var retainedLength = Math.Min(totalRead, maximumBytes);

      Array.Resize(ref buffer, retainedLength);

      return new LimitedDownload(buffer, truncated);
   }

   private static bool StartsWithClosingPunctuation(string value)
   {
      if (value.Length == 0)
      {
         return false;
      }

      return value[0] is '.' or ',' or ':' or ';' or '!' or '?' or ')' or ']' or '}';
   }

   private static Uri ValidateAndNormalizeUri(Uri uri)
   {
      if (!uri.IsAbsoluteUri)
      {
         throw new ArgumentException("The web address must be an absolute URL.");
      }

      if (uri.AbsoluteUri.Length > MaximumUrlLength)
      {
         throw new ArgumentException($"The URL cannot exceed {MaximumUrlLength} characters.");
      }

      if ((uri.Scheme != Uri.UriSchemeHttp) && (uri.Scheme != Uri.UriSchemeHttps))
      {
         throw new ArgumentException("Only HTTP and HTTPS URLs are supported.");
      }

      if (string.IsNullOrWhiteSpace(uri.Host))
      {
         throw new ArgumentException("The URL does not contain a host.");
      }

      if (!string.IsNullOrEmpty(uri.UserInfo))
      {
         throw new ArgumentException("URLs containing embedded usernames or passwords are blocked.");
      }

      var expectedPort = uri.Scheme == Uri.UriSchemeHttps ? 443 : 80;

      if (uri.Port != expectedPort)
      {
         throw new ArgumentException($"Port {uri.Port} is blocked. Web fetch currently permits " + "only HTTP port 80 and HTTPS port 443.");
      }

      var normalizedHost = uri.IdnHost.TrimEnd('.').ToLowerInvariant();

      if (IsBlockedHostName(normalizedHost))
      {
         throw new ArgumentException($"Access to local hostname '{uri.Host}' is blocked.");
      }

      if (IPAddress.TryParse(uri.Host, out var literalAddress) && IsBlockedAddress(literalAddress))
      {
         throw new ArgumentException($"Access to local, private, or reserved address " + $"'{literalAddress}' is blocked.");
      }

      var builder = new UriBuilder(uri) { Fragment = string.Empty };

      return builder.Uri;
   }

   #endregion

   #region Nested Types

   private sealed record LimitedDownload(byte[] Content, bool Truncated);

   #endregion
}