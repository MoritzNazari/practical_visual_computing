using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CallApi : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;

    void Start()
    {
        // Key aus lokaler, nicht-committeter Datei laden
        string keyPath = Path.Combine(Application.streamingAssetsPath, "openai_key.txt");
        apiKey = File.ReadAllText(keyPath).Trim();
    }

    public IEnumerator SendImageToOpenAI(byte[] imageBytes, System.Action<string> onResult)
    {
        string base64Image = System.Convert.ToBase64String(imageBytes);
        string prompt = "Analysiere dieses Bild und gib die wichtigsten Bildbestandteile mit " +
                         "vorherrschendem Molekül zurück. Antworte NUR mit JSON in diesem Format: " +
                         "[{\"label\": \"...\", \"molecule\": \"...\", \"smiles\": \"...\", \"point\": [x, y]}]";

        string jsonBody = $@"{{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": [
                        {{ ""type"": ""text"", ""text"": ""{EscapeJson(prompt)}"" }},
                        {{ ""type"": ""image_url"", ""image_url"": {{ ""url"": ""data:image/jpeg;base64,{base64Image}"" }} }}
                    ]
                }}
            ],
            ""max_tokens"": 1000
        }}";

        using UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"OpenAI Fehler: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        onResult?.Invoke(request.downloadHandler.text);
    }

    private string EscapeJson(string s) => s.Replace("\"", "\\\"");
}