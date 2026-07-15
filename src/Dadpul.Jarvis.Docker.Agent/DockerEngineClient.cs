// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Agent;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dadpul.Jarvis.Docker.Contracts;

internal sealed class DockerEngineClient
{
   #region Constants and Fields

   private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

   private readonly HttpClient httpClient;

   private readonly SemaphoreSlim versionLock = new(1, 1);

   private string? apiVersion;

   #endregion

   #region Constructors and Destructors

   public DockerEngineClient(HttpClient httpClient)
   {
      this.httpClient = httpClient;
   }

   #endregion

   #region Public Methods and Operators

   public async Task<IReadOnlyList<DockerContainerInfo>> ListContainersAsync(bool includeStopped, CancellationToken cancellationToken)
   {
      var path = await CreateApiPathAsync($"/containers/json?all={includeStopped.ToString().ToLowerInvariant()}", cancellationToken);

      using var response = await httpClient.GetAsync(path, cancellationToken);

      response.EnsureSuccessStatusCode();

      var containers = await response.Content.ReadFromJsonAsync<List<EngineContainer>>(JsonOptions, cancellationToken) ?? [];

      return containers.Select(MapContainer).ToArray();
   }

   public async Task PingAsync(CancellationToken cancellationToken)
   {
      using var response = await httpClient.GetAsync("/_ping", cancellationToken);

      response.EnsureSuccessStatusCode();
   }

   public async Task RestartContainerAsync(string containerId, int timeoutSeconds, CancellationToken cancellationToken)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

      var encodedId = Uri.EscapeDataString(containerId);

      var path = await CreateApiPathAsync($"/containers/{encodedId}/restart?t={timeoutSeconds}", cancellationToken);

      using var response = await httpClient.PostAsync(path, content: null, cancellationToken);

      if (response.StatusCode == HttpStatusCode.NotFound)
      {
         throw new DockerContainerNotFoundException(containerId);
      }

      response.EnsureSuccessStatusCode();
   }

   public async Task<bool> StartContainerAsync(string containerId, int timeoutSeconds, CancellationToken cancellationToken)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

      var encodedId = Uri.EscapeDataString(containerId);

      var path = await CreateApiPathAsync($"/containers/{encodedId}/start?t={timeoutSeconds}", cancellationToken);

      using var response = await httpClient.PostAsync(path, content: null, cancellationToken);

      if (response.StatusCode == HttpStatusCode.NotModified)
      {
         return false;
      }

      if (response.StatusCode == HttpStatusCode.NotFound)
      {
         throw new DockerContainerNotFoundException(containerId);
      }

      response.EnsureSuccessStatusCode();

      return true;
   }

   public async Task<bool> StopContainerAsync(string containerId, int timeoutSeconds, CancellationToken cancellationToken)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

      var encodedId = Uri.EscapeDataString(containerId);

      var path = await CreateApiPathAsync($"/containers/{encodedId}/stop?t={timeoutSeconds}", cancellationToken);

      using var response = await httpClient.PostAsync(path, content: null, cancellationToken);

      if (response.StatusCode == HttpStatusCode.NotModified)
      {
         return false;
      }

      if (response.StatusCode == HttpStatusCode.NotFound)
      {
         throw new DockerContainerNotFoundException(containerId);
      }

      response.EnsureSuccessStatusCode();

      return true;
   }

   #endregion

   #region Methods

   private static DockerContainerInfo MapContainer(EngineContainer container)
   {
      var names = container.Names?.Select(name => name.TrimStart('/')).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray() ?? [];

      return new DockerContainerInfo(Id: container.Id, Names: names, Image: container.Image, State: container.State, Status: container.Status,
         Labels: container.Labels ?? new Dictionary<string, string>());
   }

   static async Task<( DockerContainerInfo? Container, IResult? Error)> ResolveManagedContainerAsync(string containerReference,
      DockerEngineClient docker, DockerAgentOptions agentOptions, CancellationToken cancellationToken)
   {
      var containers = await docker.ListContainersAsync(includeStopped: true, cancellationToken);

      var matches = containers.Where(container =>
         container.Id.Equals(containerReference, StringComparison.OrdinalIgnoreCase)
         || container.Id.StartsWith(containerReference, StringComparison.OrdinalIgnoreCase)
         || container.Names.Any(name => name.Equals(containerReference, StringComparison.OrdinalIgnoreCase))).ToArray();

      if (matches.Length == 0)
      {
         return (null, Results.NotFound(new { error = $"Container '{containerReference}' was not found." }));
      }

      if (matches.Length > 1)
      {
         return (null, Results.Conflict(new { error = $"Container reference '{containerReference}' is ambiguous." }));
      }

      var container = matches[0];

      var isManaged = container.Labels.TryGetValue(agentOptions.ManagementLabel, out var labelValue)
                      && string.Equals(labelValue, agentOptions.ManagementLabelValue, StringComparison.OrdinalIgnoreCase);

      if (!isManaged)
      {
         var containerName = container.Names.FirstOrDefault() ?? container.Id;

         return (null,
            Results.Json(
               new
               {
                  error = $"Container '{containerName}' does not have the required " + $"label '{agentOptions.ManagementLabel}="
                                                                                     + $"{agentOptions.ManagementLabelValue}'."
               }, statusCode: StatusCodes.Status403Forbidden));
      }

      return (container, null);
   }

   private async Task<string> CreateApiPathAsync(string path, CancellationToken cancellationToken)
   {
      var version = await GetApiVersionAsync(cancellationToken);
      return $"/v{version}{path}";
   }

   private async Task<string> GetApiVersionAsync(CancellationToken cancellationToken)
   {
      if (apiVersion is not null)
      {
         return apiVersion;
      }

      await versionLock.WaitAsync(cancellationToken);

      try
      {
         if (apiVersion is not null)
         {
            return apiVersion;
         }

         using var response = await httpClient.GetAsync("/version", cancellationToken);

         response.EnsureSuccessStatusCode();

         var version = await response.Content.ReadFromJsonAsync<EngineVersion>(JsonOptions, cancellationToken);

         if (string.IsNullOrWhiteSpace(version?.ApiVersion))
         {
            throw new InvalidOperationException("The Docker daemon did not return an API version.");
         }

         apiVersion = version.ApiVersion;
         return apiVersion;
      }
      finally
      {
         versionLock.Release();
      }
   }

   #endregion

   private sealed record EngineVersion(
      [property: JsonPropertyName("ApiVersion")]
      string ApiVersion);

   private sealed record EngineContainer(
      [property: JsonPropertyName("Id")] string Id,
      [property: JsonPropertyName("Names")] string[]? Names,
      [property: JsonPropertyName("Image")] string Image,
      [property: JsonPropertyName("State")] string State,
      [property: JsonPropertyName("Status")] string Status,
      [property: JsonPropertyName("Labels")] Dictionary<string, string>? Labels);
}