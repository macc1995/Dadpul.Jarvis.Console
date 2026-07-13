// Bonjour

namespace Dadpul.Jarvis.Tools.Memory;

using System.ComponentModel.Composition;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class MemoryTool : ITool
{
   #region Constants and Fields

   private readonly IMemoryStore memoryStore;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public MemoryTool(IMemoryStore memoryStore)
   {
      this.memoryStore = memoryStore;
   }

   #endregion

   #region ITool Members

   public string Name => "memory_manage";

   public string Description =>
      "Stores, searches, or deletes persistent user memories. " + "Use this when the user explicitly asks to remember, recall, or forget "
                                                                + "information. Do not claim a memory was stored unless this tool succeeds.";

   public JsonObject Parameters =>
      new()
      {
         ["type"] = "object",
         ["properties"] = new JsonObject
         {
            ["operation"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray { "remember", "search", "forget" } },
            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "The complete fact to store when remembering." },
            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "The text to search for when recalling memories." },
            ["memoryId"] = new JsonObject { ["type"] = "string", ["description"] = "The exact memory ID to delete when forgetting." }
         },
         ["required"] = new JsonArray { "operation" }
      };

   public async Task<ToolResult> ExecuteAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "operation", out var operation) || string.IsNullOrWhiteSpace(operation))
      {
         return ToolResult.Failed("The required string argument 'operation' was missing or invalid.");
      }

      if (string.IsNullOrWhiteSpace(operation))
      {
         return ToolResult.Failed("The required argument 'operation' was missing.");
      }

      return operation.ToLowerInvariant() switch
      {
         "remember" => await RememberAsync(arguments, cancellationToken),

         "search" => await SearchAsync(arguments, cancellationToken),

         "forget" => await ForgetAsync(arguments, cancellationToken),

         _ => ToolResult.Failed($"Unsupported memory operation '{operation}'.")
      };
   }

   #endregion

   #region Methods

   private static bool TryGetString(JsonObject arguments, string propertyName, out string? value)
   {
      value = null;

      return arguments[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
   }

   private async Task<ToolResult> ForgetAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      var memoryIdValue = arguments["memoryId"]?.GetValue<string>();

      if (string.IsNullOrWhiteSpace(memoryIdValue))
      {
         return ToolResult.Failed("The 'memoryId' argument is required when forgetting a memory.");
      }

      if (!Guid.TryParse(memoryIdValue, out var memoryId))
      {
         return ToolResult.Failed($"'{memoryIdValue}' is not a valid memory ID.");
      }

      var deleted = await memoryStore.DeleteAsync(memoryId, cancellationToken);

      if (!deleted)
      {
         return ToolResult.Failed($"No memory with ID '{memoryId}' was found.");
      }

      return ToolResult.Successful($"Memory '{memoryId}' was deleted successfully.");
   }

   private async Task<ToolResult> RememberAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      var content = arguments["content"]?.GetValue<string>();

      if (string.IsNullOrWhiteSpace(content))
      {
         return ToolResult.Failed("The 'content' argument is required when remembering something.");
      }

      var memory = await memoryStore.StoreAsync(content.Trim(), cancellationToken);

      return ToolResult.Successful($"""
                                    Memory stored successfully.

                                    Memory ID: {memory.Id}
                                    Content: {memory.Content}
                                    """);
   }

   private async Task<ToolResult> SearchAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      var query = arguments["query"]?.GetValue<string>();

      if (string.IsNullOrWhiteSpace(query))
      {
         return ToolResult.Failed("The 'query' argument is required when searching memories.");
      }

      var memories = await memoryStore.SearchAsync(query.Trim(), cancellationToken);

      if (memories.Count == 0)
      {
         return ToolResult.Successful("No matching memories were found.");
      }

      var formattedMemories = string.Join(Environment.NewLine,
         memories.Select(memory => $"- ID: {memory.Id}{Environment.NewLine}" + $"  Content: {memory.Content}"));

      return ToolResult.Successful($"""
                                    Matching memories:

                                    {formattedMemories}
                                    """);
   }

   #endregion
}