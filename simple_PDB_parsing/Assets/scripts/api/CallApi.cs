using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Encodings.Web;
using Unity.AI.MCP.Editor.Helpers;

public class CallApi : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/responses";
    private string apiKey;

    void Start()
    {
        // Key aus lokaler, nicht-committeter Datei laden
        string keyPath = Path.Combine(Application.streamingAssetsPath, "openai_key.txt");
        apiKey = File.ReadAllText(keyPath).Trim();

        StartCoroutine(getExistingResponse("resp_0cddc779758934a8006a84d264c244819d94511fb77fd59bd2"));
    }

    public IEnumerator getExistingResponse(string respId)
    {
        // Test for getting an existing response via its ID
        string respRetrUrl = ApiUrl + "/" + respId;
        using UnityWebRequest testRequest = new UnityWebRequest(respRetrUrl, "GET");
        testRequest.downloadHandler = new DownloadHandlerBuffer();
        testRequest.SetRequestHeader("Content-Type", "application/json");
        testRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log($"Sending WebRequest to following url {respRetrUrl}");

        yield return testRequest.SendWebRequest();

        if (testRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OpenAI Fehler: {testRequest.error}\n{testRequest.downloadHandler.text}");
            yield break;
        }

        string rawResponse = testRequest.downloadHandler.text;

        Debug.Log($"{rawResponse}");
    }

    // We could think about splitting the prompt into system and user prompt, as of now this is sufficient though
    // Would work like this:
    // "input": [ { ... }, { ... } ] is an Array, so there can be multiple elements each with their own role and content
    public IEnumerator SendImageToOpenAI(byte[] imageBytes, System.Action<string> onResult)
    {
        const string modelName = "gpt-4.1-nano";
        string base64Image = System.Convert.ToBase64String(imageBytes);
        string prompt = "Analysiere dieses Bild und gib die wichtigsten Bildbestandteile mit " +
                         "vorherrschendem Molekül zurück. Antworte NUR mit JSON im angegebenen structured Format";

        string jsonBody = $@"{{
            ""model"": ""{modelName}"",
            ""input"": [
                {{
                    ""role"": ""user"",
                    ""content"": [
                        {{ ""type"": ""input_text"", ""text"": ""{EscapeJson(prompt)}"" }},
                        {{ ""type"": ""input_image"", ""image_url"": ""data:image/jpeg;base64,{base64Image}"" }}
                    ]
                }}
            ],
            ""text"": 
                {{
                    ""format"":  
                    {{
                        ""type"": ""json_schema"",
                        ""name"": ""image_components"",
                        ""strict"": true,
                        ""schema"": 
                        {{
                            ""type"": ""object"",
                            ""properties"":
                            {{
                                ""component_list"":
                                {{
                                    ""type"": ""array"",
                                    ""items"": 
                                    {{
                                        ""type"": ""object"",
                                        ""properties"": 
                                        {{
                                            ""label"": {{ ""type"": ""string"" }},
                                            ""molecule"": {{ ""type"": ""string"" }},
                                            ""smiles"": 
                                            {{ 
                                                ""type"": ""string"", 
                                                ""minLength"":   1,
                                                ""pattern"": ""^[A-Za-z0-9@+\\-\\[\\]()=#$:./\\\\%]+$""
                                            }},
                                            ""point"": 
                                            {{ 
                                                ""type"": ""object"",
                                                ""properties"": 
                                                {{ 
                                                    ""x"": {{ ""type"": ""number"", ""minimum"": 0, ""maximum"": 1 }},
                                                    ""y"": {{ ""type"": ""number"", ""minimum"": 0, ""maximum"": 1 }}
                                                }},
                                                ""required"": [""x"", ""y""],
                                                ""additionalProperties"": false
                                            }}
                                        }},
                                        ""required"": [""label"", ""molecule"", ""smiles"", ""point""],
                                        ""additionalProperties"": false
                                    }}
                                }}
                            }},
                            ""required"": [""component_list""],
                            ""additionalProperties"": false
                        }}
                    }}
                }},
            ""max_output_tokens"": 1000
        }}";

        using UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log($"Sending WebRequest with following data: {jsonBody}");

        //Send Request and run stopwatch
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        yield return request.SendWebRequest();
        stopwatch.Stop();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OpenAI Fehler: {request.error}\n{request.downloadHandler.text}");

            ApiCallLogger.logCall(
                endpoint: $"{modelName}/image-analysis",
                request: jsonBody,
                response: null,
                success: false,
                latencyMs: stopwatch.Elapsed.TotalMilliseconds,
                error: request.error,
                errorText: request.downloadHandler.text
                );
            yield break;
        }

        string rawResponse = request.downloadHandler.text;

        ApiCallLogger.logCall(
            endpoint: $"{modelName}/image-analysis",
            request: jsonBody,
            response: rawResponse,
            success: true,
            latencyMs: stopwatch.Elapsed.TotalMilliseconds
        );

        onResult?.Invoke(ExtractOutputTextOrRaw(rawResponse));
    }

    // Responses API returns a JSON envelope; extract plain model text if present.
    private string ExtractOutputTextOrRaw(string rawResponse)
    {
        Match match = Regex.Match(rawResponse, "\"output_text\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
        if (!match.Success) return rawResponse;

        string escapedText = match.Groups[1].Value;
        string unescapedText = Regex.Unescape(escapedText);
        return unescapedText;
    }

    private string EscapeJson(string s) => s.Replace("\"", "\\\"");
}