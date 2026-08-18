using System;
using System.IO;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;

public static class ApiCallLogger
{
    private static readonly string logPath =
    Path.Combine(Application.persistentDataPath, "api_calls.jsonl");

    private static readonly object _lock = new object();

    private class ApiCallEntry
    {
        public string timestamp;
        public string endpoint;
        public string request;
        public string response;
        public bool success;
        public double latencyMs;
        public string error;
        public string errorText;
    }

    public static void logCall(
        string endpoint,
        string request,
        string response,
        bool success,
        double latencyMs,
        string error = null,
        string errorText = null)
    {
        ApiCallEntry entry = new ApiCallEntry()
        {
            timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601
            endpoint = endpoint,
            request = request,
            response = response,
            success = success,
            latencyMs = latencyMs,
            error = error,
            errorText = errorText
        };

        string json = JsonConvert.SerializeObject(entry);

        lock (_lock)
        {
            File.AppendAllText(logPath, json + Environment.NewLine);
        }
    }
}