// Bonjour

namespace Dadpul.Jarvis.Tools.Memory;

using System.ComponentModel.Composition;
using System.Text.Json.Nodes;

using Dadpul.Jarvis.Interfaces.Tools;

[Export(typeof(ITool))]
internal sealed class MemoryTool : ITool
{
   #region Constants and Fields

   private readonly IMemoryRetriever memoryRetriever;

   private readonly IMemoryService memoryService;

   #endregion

   #region Constructors and Destructors

   [ImportingConstructor]
   public MemoryTool(IMemoryService memoryService, IMemoryRetriever memoryRetriever)
   {
      this.memoryService = memoryService;
      this.memoryRetriever = memoryRetriever;
   }

   #endregion

   #region ITool Members

   public string Name => "memory_manage";

   public string Description =>
      "Stores, searches, or deletes persistent user memories. " + "Use this when the user explicitly asks to remember, recall, or forget "
                                                                + "information. Do not claim a memory was stored unless this tool succeeds."
                                                                + "User 'remember' operation to store memories, 'search' operation to recall memories, and 'forget' operation to delete memories.";

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

   private static string BuildAmbiguousResult(IReadOnlyList<MemoryMatch> candidates)
   {
      var candidateText = string.Join(Environment.NewLine, candidates.Select(match => $"- {match.Memory.Content}"));

      return $"""
              Multiple memories matched the request and none was clearly best.

              Candidates:
              {candidateText}

              Ask the user to clarify which memory should be forgotten.
              """;
   }

   private static bool TryGetString(JsonObject arguments, string propertyName, out string? value)
   {
      value = null;

      return arguments[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
   }

   private async Task<ToolResult> ForgetAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "query", out var query) || string.IsNullOrWhiteSpace(query))
      {
         return ToolResult.Failed("The required string argument 'query' was missing or invalid.");
      }

      var result = await memoryService.ForgetAsync(query.Trim(), cancellationToken);

      return result.Status switch
      {
         ForgetMemoryStatus.Deleted => ToolResult.Successful($"""
                                                              Memory deleted successfully.

                                                              Deleted content: {result.DeletedMemory!.Content}
                                                              """),

         ForgetMemoryStatus.NotFound => ToolResult.Failed("No memory matched the request confidently enough to delete."),

         ForgetMemoryStatus.Ambiguous => ToolResult.Failed(BuildAmbiguousResult(result.Candidates)),

         _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, "Unsupported forget-memory status.")
      };
   }

   private async Task<ToolResult> RememberAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "content", out var content) || string.IsNullOrWhiteSpace(content))
      {
         return ToolResult.Failed("The required string argument 'content' was missing or invalid.");
      }

      var memory = await memoryService.RememberAsync(content.Trim(), cancellationToken);

      return ToolResult.Successful($"""
                                    Memory stored successfully.

                                    Content: {memory.Content}
                                    """);
   }

   private async Task<ToolResult> SearchAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (!TryGetString(arguments, "query", out var query) || string.IsNullOrWhiteSpace(query))
      {
         return ToolResult.Failed("The required string argument 'query' was missing or invalid.");
      }

      var matches = await memoryRetriever.RetrieveAsync(query.Trim(), cancellationToken);

      if (matches.Count == 0)
      {
         return ToolResult.Successful("No relevant memories were found.");
      }

      var formattedMemories = string.Join(Environment.NewLine, matches.Select(match => $"""
                                                                                        - Memory ID: {match.Memory.Id}
                                                                                          Similarity: {match.Similarity:F4}
                                                                                          Content: {match.Memory.Content}
                                                                                        """));

      return ToolResult.Successful($"""
                                    Relevant memories:

                                    {formattedMemories}
                                    """);
   }

   #endregion
}