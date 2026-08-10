using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoleculeLoaderUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button smilesButton;
    [SerializeField] private Button nameButton;
    [SerializeField] private manager moleculeManager; // Name ggf. anpassen

/// <summary>
/// Die MoleculeSourceType-Enumeration definiert die möglichen Eingabetypen für die Molekül-Ladefunktion.
/// </summary>
    public enum MoleculeSourceType { Name, Smiles }

    void Start()
    {
        smilesButton.onClick.AddListener(() => OnLoadButtonClicked(MoleculeSourceType.Smiles));
        nameButton.onClick.AddListener(() => OnLoadButtonClicked(MoleculeSourceType.Name));
    }

    private void OnLoadButtonClicked(MoleculeSourceType mode)
    {
        string input = inputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            Debug.LogWarning("Eingabefeld ist leer!");
            return;
        }

        StartCoroutine(moleculeManager.LoadMolecule(input, mode));
    }
}