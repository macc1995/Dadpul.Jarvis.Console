// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application;

using System.Text.Json.Nodes;

internal sealed record ToolExecutionRecord(string ToolName, JsonObject Arguments, bool Success, string Result);