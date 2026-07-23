// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.ComponentModel.Composition;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using Dadpul.Jarvis.Docker.Contracts;

using Microsoft.Extensions.Configuration;

[Export(typeof(IDockerController))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed partial class DockerController : IDockerController, IDisposable
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private readonly Dictionary<string, DockerNodeClient> nodes;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public DockerController(IConfiguration configuration)
   {
      ArgumentNullException.ThrowIfNull(configuration);

      var options = configuration.GetRequiredSection(DockerOptions.SectionName).Get<DockerOptions>()
                    ?? throw new DockerControllerException("Docker configuration could not be bound.");

      if (options.Nodes.Count == 0)
      {
         throw new DockerControllerException("No Docker nodes are configured.");
      }

      if (options.RequestTimeoutSeconds <= 0)
      {
         throw new DockerControllerException("Docker:RequestTimeoutSeconds must be greater than zero.");
      }

      nodes = options.Nodes.Values.ToDictionary(node => GetConfiguredNodeName(node), node => CreateNodeClient(node, options.RequestTimeoutSeconds),
         StringComparer.OrdinalIgnoreCase);
   }

   #endregion

   #region IDisposable Members

   public void Dispose()
   {
      foreach (var node in nodes.Values)
      {
         node.HttpClient.Dispose();
      }
   }

   #endregion

   #region IDockerController Members

   public async Task<DockerContainerSearchResult> SearchContainersAsync(string? node, string? query, bool includeStopped,
      CancellationToken cancellationToken)
   {
      var selectedNodes = string.IsNullOrWhiteSpace(node) ? nodes.Keys.OrderBy(name => name).ToArray() : [GetNodeName(node)];

      var operations = selectedNodes.Select(async nodeName =>
      {
         try
         {
            var containers = await ListContainersFromNodeAsync(nodeName, includeStopped, cancellationToken);

            return new NodeSearchResult(Node: nodeName, Containers: containers, Error: null);
         }
         catch (Exception exception)
         {
            return new NodeSearchResult(Node: nodeName, Containers: [], Error: exception.Message);
         }
      });

      var nodeResults = await Task.WhenAll(operations);

      var containers = nodeResults.Where(result => result.Error is null)
         .SelectMany(result => result.Containers.Select(container => new LocatedDockerContainer(result.Node, container)))
         .Where(container => MatchesQuery(container.Container, query)).ToArray();

      var failedNodes = nodeResults.Where(result => result.Error is not null).Select(result => new DockerNodeFailure(result.Node, result.Error!))
         .ToArray();

      return new DockerContainerSearchResult(Containers: containers, FailedNodes: failedNodes);
   }

   public IReadOnlyCollection<string> NodeNames
   {
      get
      {
         return nodes.Keys.OrderBy(name => name).ToArray();
      }
   }

   public Task<DockerActionResult> RestartContainerAsync(string node, string containerName, CancellationToken cancellationToken)
   {
      return SendContainerActionAsync(node, containerName, "restart", cancellationToken);
   }

   public Task<DockerActionResult> StopContainerAsync(string node, string containerName, CancellationToken cancellationToken)
   {
      return SendContainerActionAsync(node, containerName, "stop", cancellationToken);
   }

   public Task<DockerActionResult> StartContainerAsync(string node, string containerName, CancellationToken cancellationToken)
   {
      return SendContainerActionAsync(node, containerName, "start", cancellationToken);
   }

   #endregion

   #region Methods

   private static void AddLabelValue(DockerContainerInfo container, ICollection<string> values, string label)
   {
      if (container.Labels.TryGetValue(label, out var value) && !string.IsNullOrWhiteSpace(value))
      {
         values.Add(value);
      }
   }

   private static DockerNodeClient CreateNodeClient(DockerNodeOptions configuration, int requestTimeoutSeconds)
   {
      if (string.IsNullOrWhiteSpace(configuration.Name))
      {
         throw new DockerControllerException("A Docker node has no name.");
      }

      if (configuration.BaseUrl is null)
      {
         throw new DockerControllerException($"Docker node '{configuration.Name}' has no BaseUrl.");
      }

      if ((configuration.BaseUrl.Scheme != Uri.UriSchemeHttp) && (configuration.BaseUrl.Scheme != Uri.UriSchemeHttps))
      {
         throw new DockerControllerException($"Docker node '{configuration.Name}' must use HTTP or HTTPS.");
      }

      if (string.IsNullOrWhiteSpace(configuration.ApiKey))
      {
         throw new DockerControllerException($"Docker node '{configuration.Name}' has no API key.");
      }

      var apiKey = configuration.ApiKey;

      var httpClient = new HttpClient
      {
         BaseAddress = EnsureTrailingSlash(configuration.BaseUrl), Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds)
      };

      return new DockerNodeClient(httpClient, apiKey);
   }

   private static HttpRequestMessage CreateRequest(DockerNodeClient node, HttpMethod method, string relativeUrl)
   {
      var request = new HttpRequestMessage(method, relativeUrl);

      request.Headers.TryAddWithoutValidation("X-Jarvis-Key", node.ApiKey);

      return request;
   }

   private static async Task EnsureSuccessfulAsync(string node, HttpResponseMessage response, CancellationToken cancellationToken)
   {
      if (response.IsSuccessStatusCode)
      {
         return;
      }

      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

      throw new DockerControllerException($"Docker node '{node}' returned " + $"{(int)response.StatusCode} {response.ReasonPhrase}. " + responseBody);
   }

   private static Uri EnsureTrailingSlash(Uri uri)
   {
      var value = uri.AbsoluteUri.EndsWith('/') ? uri.AbsoluteUri : uri.AbsoluteUri + "/";

      return new Uri(value);
   }

   private static string GetConfiguredNodeName(DockerNodeOptions configuration)
   {
      if (string.IsNullOrWhiteSpace(configuration.Name))
      {
         throw new DockerControllerException("A Docker node has no name.");
      }

      return configuration.Name;
   }

   private static bool MatchesQuery(DockerContainerInfo container, string? query)
   {
      if (string.IsNullOrWhiteSpace(query))
      {
         return true;
      }

      var searchableValues = new List<string>();

      searchableValues.AddRange(container.Names);
      searchableValues.Add(container.Image);

      AddLabelValue(container, searchableValues, "jarvis.display-name");

      AddLabelValue(container, searchableValues, "com.docker.compose.project");

      AddLabelValue(container, searchableValues, "com.docker.compose.service");

      if (container.Labels.TryGetValue("jarvis.aliases", out var aliases))
      {
         searchableValues.AddRange(aliases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
      }

      var normalizedQuery = Normalize(query);
      var queryTokens = Tokenize(query);

      return searchableValues.Any(candidate =>
      {
         var normalizedCandidate = Normalize(candidate);

         // Exact normalized match.
         if (normalizedCandidate.Equals(normalizedQuery, StringComparison.Ordinal))
         {
            return true;
         }

         // Ordinary partial name match.
         if (normalizedCandidate.Contains(normalizedQuery, StringComparison.Ordinal))
         {
            return true;
         }

         /*
          * Token-subset match:
          *
          * "jarvis-test"
          *     → ["jarvis", "test"]
          *
          * "jarvis-agent-test"
          *     → ["jarvis", "agent", "test"]
          *
          * Every query token exists in the candidate, so this matches.
          */
         var candidateTokens = Tokenize(candidate);

         return (queryTokens.Length > 0) && queryTokens.All(queryToken =>
            candidateTokens.Any(candidateToken => candidateToken.Equals(queryToken, StringComparison.Ordinal)));
      });
   }

   private static string Normalize(string value)
   {
      return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
   }

   private static string[] Tokenize(string value)
   {
      return Regex.Matches(value.ToLowerInvariant(), @"[\p{L}\p{N}]+").Select(match => match.Value).Where(token => token.Length > 0).ToArray();
   }

   private DockerNodeClient GetNode(string node)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(node);

      if (nodes.TryGetValue(node, out var nodeClient))
      {
         return nodeClient;
      }

      throw new DockerControllerException($"Docker node '{node}' is not configured. " + $"Available nodes: {string.Join(", ", NodeNames)}.");
   }

   private string GetNodeName(string node)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(node);

      if (nodes.Keys.FirstOrDefault(name => name.Equals(node, StringComparison.OrdinalIgnoreCase)) is { } found)
      {
         return found;
      }

      throw new DockerControllerException($"Docker node '{node}' is not configured. " + $"Available nodes: {string.Join(", ", NodeNames)}.");
   }

   private async Task<IReadOnlyList<DockerContainerInfo>> ListContainersFromNodeAsync(string node, bool includeStopped,
      CancellationToken cancellationToken)
   {
      var nodeClient = GetNode(node);

      using var request = CreateRequest(nodeClient, HttpMethod.Get, "api/containers?includeStopped=" + includeStopped.ToString().ToLowerInvariant());

      using var response = await nodeClient.HttpClient.SendAsync(request, cancellationToken);

      await EnsureSuccessfulAsync(node, response, cancellationToken);

      return await response.Content.ReadFromJsonAsync<List<DockerContainerInfo>>(JsonOptions, cancellationToken) ?? [];
   }

   private async Task<DockerActionResult> SendContainerActionAsync(string node, string containerName, string action,
      CancellationToken cancellationToken)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

      var nodeClient = GetNode(node);
      var encodedName = Uri.EscapeDataString(containerName);

      using var request = CreateRequest(nodeClient, HttpMethod.Post, $"api/containers/{encodedName}/{action}");

      using var response = await nodeClient.HttpClient.SendAsync(request, cancellationToken);

      await EnsureSuccessfulAsync(node, response, cancellationToken);

      return await response.Content.ReadFromJsonAsync<DockerActionResult>(JsonOptions, cancellationToken)
             ?? throw new DockerControllerException($"Node '{node}' returned an empty {action} response.");
   }

   #endregion

   private sealed record DockerNodeClient(HttpClient HttpClient, string ApiKey);
}