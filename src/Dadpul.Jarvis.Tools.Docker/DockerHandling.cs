// Made by Dadpul

namespace Dadpul.Jarvis.Tools.Docker;

using System.Text.Json.Nodes;

using Dadpul.Jarvis.Docker.Contracts;

public class DockerHandling
{
   #region Constants and Fields

   internal readonly IDockerController dockerController;

   #endregion

   #region Constructors and Destructors

   public DockerHandling(IDockerController dockerController)
   {
      this.dockerController = dockerController;
   }

   #endregion

   #region Methods

   internal static bool TryGetString(JsonObject arguments, string name, out string value)
   {
      value = string.Empty;

      if (arguments[name] is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var result) || string.IsNullOrWhiteSpace(result))
      {
         return false;
      }

      value = result;
      return true;
   }

   internal JsonArray CreateNodeArray()
   {
      return new JsonArray(dockerController.NodeNames.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
   }

   #endregion
}