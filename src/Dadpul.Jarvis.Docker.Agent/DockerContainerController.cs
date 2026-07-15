// Made by Dadpul

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Dadpul.Jarvis.Docker.Agent
{
   using Dadpul.Jarvis.Docker.Contracts;

   using Microsoft.AspNetCore.Mvc;
   using Microsoft.Extensions.Options;

   //[Route("api/[controller]")]
   [ApiController]
   public class DockerContainerController : ControllerBase
   {
      #region Constants and Fields

      private readonly DockerAgentOptions agentOptions;

      #endregion

      #region Constructors and Destructors

      public DockerContainerController(IOptions<DockerAgentOptions> options)
      {
         agentOptions = options.Value;
      }

      #endregion

      #region Public Methods and Operators

      [HttpGet("/api/containers")]
      public async Task<IActionResult> Containers(bool? includeStopped, CancellationToken cancellationToken)
      {
         var docker = HttpContext.RequestServices.GetRequiredService<DockerEngineClient>();
         var containers = await docker.ListContainersAsync(includeStopped ?? true, cancellationToken);

         return Ok(containers);
      }

      [HttpGet("/health/docker")]
      public async Task<IActionResult> Docker(CancellationToken cancellationToken)
      {
         var docker = HttpContext.RequestServices.GetRequiredService<DockerEngineClient>();

         try
         {
            await docker.PingAsync(cancellationToken);

            return Ok(new { status = "healthy", node = agentOptions.NodeName, docker = "reachable" });
         }
         catch (Exception exception)
         {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
               new { status = "unhealthy", node = agentOptions.NodeName, docker = "unreachable", error = exception.Message });
         }
      }

      [HttpGet("/health/live")]
      public IActionResult Get()
      {
         return Ok(new { status = "healthy", node = agentOptions.NodeName });
      }

      [HttpPost("/api/containers/{containerReference}/restart")]
      public async Task<IActionResult> Restart(string containerReference, CancellationToken cancellationToken)
      {
         var docker = HttpContext.RequestServices.GetRequiredService<DockerEngineClient>();
         var resolution = await ResolveManagedContainerAsync(containerReference, docker, agentOptions, cancellationToken);

         if (resolution.Error is not null)
         {
            return resolution.Error;
         }

         var container = resolution.Container!;

         await docker.RestartContainerAsync(container.Id, agentOptions.RestartTimeoutSeconds, cancellationToken);

         return Ok(new DockerActionResult(Success: true, Node: agentOptions.NodeName, ContainerId: container.Id,
            ContainerName: container.Names.FirstOrDefault() ?? container.Id, Message: "Container restarted successfully."));
      }

      [HttpPost("/api/containers/{containerReference}/start")]
      public async Task<IActionResult> Start(string containerReference, CancellationToken cancellationToken)
      {
         var docker = HttpContext.RequestServices.GetRequiredService<DockerEngineClient>();
         var resolution = await ResolveManagedContainerAsync(containerReference, docker, agentOptions, cancellationToken);

         if (resolution.Error is not null)
         {
            return resolution.Error;
         }

         var container = resolution.Container!;
         var stateChanged = await docker.StartContainerAsync(container.Id, agentOptions.StopTimeoutSeconds, cancellationToken);
         var containerName = container.Names.FirstOrDefault() ?? container.Id;

         return Ok(new DockerActionResult(Success: true, Node: agentOptions.NodeName, ContainerId: container.Id, ContainerName: containerName,
            Message: stateChanged ? "Container started successfully." : "Container was already started."));
      }

      [HttpPost("/api/containers/{containerReference}/stop")]
      public async Task<IActionResult> Stop(string containerReference, CancellationToken cancellationToken)
      {
         var docker = HttpContext.RequestServices.GetRequiredService<DockerEngineClient>();
         var resolution = await ResolveManagedContainerAsync(containerReference, docker, agentOptions, cancellationToken);

         if (resolution.Error is not null)
         {
            return resolution.Error;
         }

         var container = resolution.Container!;
         var stateChanged = await docker.StopContainerAsync(container.Id, agentOptions.StopTimeoutSeconds, cancellationToken);
         var containerName = container.Names.FirstOrDefault() ?? container.Id;

         return Ok(new DockerActionResult(Success: true, Node: agentOptions.NodeName, ContainerId: container.Id, ContainerName: containerName,
            Message: stateChanged ? "Container stopped successfully." : "Container was already stopped."));
      }

      #endregion

      #region Methods

      private static async Task<(DockerContainerInfo? Container, IActionResult? Error)> ResolveManagedContainerAsync(string containerReference,
         DockerEngineClient docker, DockerAgentOptions options, CancellationToken cancellationToken)
      {
         var containers = await docker.ListContainersAsync(includeStopped: true, cancellationToken);

         var matches = containers.Where(container =>
            container.Id.Equals(containerReference, StringComparison.OrdinalIgnoreCase)
            || container.Id.StartsWith(containerReference, StringComparison.OrdinalIgnoreCase)
            || container.Names.Any(name => name.Equals(containerReference, StringComparison.OrdinalIgnoreCase))).ToArray();

         if (matches.Length == 0)
         {
            return (null, new NotFoundObjectResult(new { error = $"Container '{containerReference}' was not found." }));
         }

         if (matches.Length > 1)
         {
            return (null, new ConflictObjectResult(new { error = $"Container reference '{containerReference}' is ambiguous." }));
         }

         var container = matches[0];

         var isManaged = container.Labels.TryGetValue(options.ManagementLabel, out var labelValue)
                         && string.Equals(labelValue, options.ManagementLabelValue, StringComparison.OrdinalIgnoreCase);

         if (!isManaged)
         {
            var containerName = container.Names.FirstOrDefault() ?? container.Id;

            return (null,
               new ObjectResult(new
               {
                  error = $"Container '{containerName}' does not have the required " + $"label '{options.ManagementLabel}="
                                                                                     + $"{options.ManagementLabelValue}'."
               }) { StatusCode = StatusCodes.Status403Forbidden });
         }

         return (container, null);
      }

      #endregion
   }
}