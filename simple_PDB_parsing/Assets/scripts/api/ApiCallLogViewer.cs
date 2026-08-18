using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class ApiCallLogViewer
{
    private static string LogPath =>
        Path.Combine(Application.persistentDataPath, "api_calls.jsonl");

    [MenuItem("Tools/API Logs/Print Summary")]
    public static void PrintSummary()
    {
        if (!File.Exists(LogPath))
        {
            Debug.LogWarning($"No log file found at {LogPath}");
            return;
        }

        var sb = new StringBuilder();
        int lineNum = 0;

        foreach (string line in File.ReadLines(LogPath))
        {
            lineNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                JObject entry = JObject.Parse(line);

                string timestamp = entry["timestamp"]?.Value<string>() ?? "N/A";
                string endpoint = entry["endpoint"]?.Value<string>() ?? "N/A";
                double? latencyMs = entry["latencyMs"]?.Value<double?>();
                int? promptTokens = entry["promptTokens"]?.Value<int?>();
                int? completionTokens = entry["completionTokens"]?.Value<int?>();

                string model = "N/A";
                int? totalTokens = null;

                string responseRaw = entry["response"]?.Value<string>();
                if (!string.IsNullOrEmpty(responseRaw))
                {
                    JObject responseJson = JObject.Parse(responseRaw);

                    // model is a top-level field on the response envelope
                    model = responseJson["model"]?.Value<string>() ?? "N/A";

                    // usage.input_tokens / usage.output_tokens / usage.total_tokens
                    // are all directly available — prefer these over the outer log fields
                    var usage = responseJson["usage"];
                    var respPromptTokens = usage?["input_tokens"]?.Value<int?>();
                    var respCompletionTokens = usage?["output_tokens"]?.Value<int?>();
                    totalTokens = usage?["total_tokens"]?.Value<int?>();

                    if (respPromptTokens.HasValue) promptTokens = respPromptTokens;
                    if (respCompletionTokens.HasValue) completionTokens = respCompletionTokens;
                }

                if (totalTokens == null && promptTokens.HasValue && completionTokens.HasValue)
                    totalTokens = promptTokens + completionTokens;

                sb.AppendLine(
                    $"[{timestamp}] endpoint={endpoint} model={model} " +
                    $"latencyMs={latencyMs?.ToString("F1") ?? "N/A"} " +
                    $"promptTokens={promptTokens?.ToString() ?? "N/A"} " +
                    $"completionTokens={completionTokens?.ToString() ?? "N/A"} " +
                    $"totalTokens={totalTokens?.ToString() ?? "N/A"}"
                );
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[line {lineNum}] Failed to parse: {ex.Message}");
            }
        }

        Debug.Log(sb.ToString());
    }
}