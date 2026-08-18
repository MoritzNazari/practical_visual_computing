using SFB;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

[RequireComponent(typeof(CallApi))]
public class ImageUploader : MonoBehaviour
{
    [SerializeField] private Button uploadButton;
    [SerializeField] private CallApi callApi; // Referenz auf euren API-Caller

    void Start()
    {
        uploadButton.onClick.AddListener(OnUploadButtonClicked);
    }

    public void OnUploadButtonClicked()
    {
        var extensions = new[] { new ExtensionFilter("Images", "png", "jpg", "jpeg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Bild auswählen", "", extensions, false);

        if (paths.Length == 0) return; // User hat abgebrochen

        string path = paths[0];
        byte[] imageBytes = File.ReadAllBytes(path);

        CallApi apiCaller = GetComponent<CallApi>();
        // Weiterreichen an den API-Call
        
        StartCoroutine(apiCaller.SendImageToOpenAI(imageBytes, OnAnalysisComplete));

        /*StartCoroutine(apiCaller.SendImageToOpenAI(imageBytes, (result) => {
            
        }));*/
    }
    private void OnAnalysisComplete(string jsonResult)
    {
        Debug.Log($"OpenAI Response: {jsonResult}");
        ApiCallLogViewer.PrintSummary();
        // hier später: JSON parsen und Punkte aufs Bild setzen
    }
}