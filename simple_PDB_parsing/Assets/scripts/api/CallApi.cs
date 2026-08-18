using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CallApi : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/responses";
    private string apiKey;

    void Start()
    {
        // Key aus lokaler, nicht-committeter Datei laden
        string keyPath = Path.Combine(Application.streamingAssetsPath, "openai_key.txt");
        apiKey = File.ReadAllText(keyPath).Trim();
    }

    // We could think about splitting the prompt into system and user prompt, as of now this is sufficient though
    // Would work like this:
    // "input": [ { ... }, { ... } ] is an Array, so there can be multiple elements each with their own role and content
    public IEnumerator SendImageToOpenAI(byte[] imageBytes, System.Action<string> onResult)
    {
        string base64Image = System.Convert.ToBase64String(imageBytes);
        string prompt = "Analysiere dieses Bild und gib die wichtigsten Bildbestandteile mit " +
                         "vorherrschendem Molekül zurück. Antworte NUR mit JSON im angegebenen structured Format";

        string jsonBody = $@"{{
            ""model"": ""gpt-4.1"",
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

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OpenAI Fehler: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        string rawResponse = request.downloadHandler.text;
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