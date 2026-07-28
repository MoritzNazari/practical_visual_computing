using System.Collections.Generic;
using UnityEngine;
using MoleculeReaderInterface;
using System.Xml;
using System;
using System.IO;
using System.Linq;


public class MoleculeLoader
{
    private readonly IEnumerable<IMoleculeReader> _readers;

    // Konstruktor-Injektion: die Liste der verfügbaren Reader kommt von außen
    public MoleculeLoader(IEnumerable<IMoleculeReader> readers)
    {
        _readers = readers;
    }

    public List<Atom> Load(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        var reader = _readers.FirstOrDefault(r => r.CanRead(extension));

        if (reader == null)
            throw new NotSupportedException($"Kein Reader für Format {extension} registriert.");

        return reader.Read(path);
    }
}


/*
public class MoleculeLoader : MonoBehaviour
{
    public string fileName = "1CRN.pdb";

    // Skalierungsfaktor, da PDB-Koordinaten in Angstrom sind und in Unity sonst kaum sichtbar waeren.
    public float scaleFactor = 1.0f;

    void Start()
    {

        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        List<Atom> atoms = SimplePDBParser.ParseFile(path);

        Debug.Log($"{atoms.Count} Atome geladen aus {fileName}");

        foreach (Atom atom in atoms)
        {
            CreateAtomSphere(atom);
        }
    }

    void CreateAtomSphere(Atom atom)
    {
        // Eine einfache Kugel pro Atom erzeugen
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Position setzen (PDB nutzt X,Y,Z in Angstrom)
        sphere.transform.position = new Vector3(atom.X, atom.Y, atom.Z) * scaleFactor;

        // Kleine, einheitliche Groesse fuer den ersten Test
        // (spaeter: Radius je nach Element unterschiedlich)
        sphere.transform.localScale = Vector3.one * 0.3f;

        // Als Kind des MoleculeManager-Objekts organisieren,
        // damit die Hierarchy nicht unuebersichtlich wird
        sphere.transform.parent = this.transform;

        // Benennung zur besseren Lesbarkeit in der Hierarchy
        sphere.name = $"{atom.Element}_{atom.SerialNumber}";

        // Einfache Farbgebung nach Element (sehr rudimentaer,
        // spaeter durch ein richtiges Farbschema ersetzen)
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material.color = GetColorForElement(atom.Element);
    }

    Color GetColorForElement(string element)
    {
        switch (element)
        {
            case "C": return Color.gray;
            case "N": return Color.blue;
            case "O": return Color.red;
            case "S": return Color.yellow;
            default: return Color.magenta; // Unbekannt/Sonstiges
        }
    }
}

*/