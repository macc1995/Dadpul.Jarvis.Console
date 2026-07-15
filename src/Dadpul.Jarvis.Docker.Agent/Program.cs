// Made by Dadpul

using System.Security.Cryptography;
using System.Text;

using Dadpul.Jarvis.Docker.Agent;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<DockerAgentOptions>().Bind(builder.Configuration.GetSection(DockerAgentOptions.SectionName))
   .Validate(options => !string.IsNullOrWhiteSpace(options.NodeName), "DockerAgent:NodeName is required.")
   .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "DockerAgent:Endpoint is required.")
   .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "DockerAgent:ApiKey is required.").ValidateOnStart();

builder.Services.AddControllers();

builder.Services.AddSingleton(serviceProvider =>
{
   var options = serviceProvider.GetRequiredService<IOptions<DockerAgentOptions>>().Value;

   return DockerEngineTransport.CreateHttpClient(options.Endpoint);
});

builder.Services.AddSingleton<DockerEngineClient>();

var app = builder.Build();

app.MapControllers();
var agentOptions = app.Services.GetRequiredService<IOptions<DockerAgentOptions>>().Value;

app.Use(async (context, next) =>
{
   if (context.Request.Path.StartsWithSegments("/health"))
   {
      await next();
      return;
   }

   var suppliedApiKey = context.Request.Headers["X-Jarvis-Key"].FirstOrDefault();

   if (!ApiKeysMatch(agentOptions.ApiKey, suppliedApiKey))
   {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });

      return;
   }

   await next();
});

//app.MapGet("/health/live", () => { return Results.Ok(new { status = "healthy", node = agentOptions.NodeName }); });

app.Run();

static bool ApiKeysMatch(string expected, string? supplied)
{
   if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied))
   {
      return false;
   }

   var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

   var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));

   return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
}