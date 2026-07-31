using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class manager : MonoBehaviour
{
       public string moleculeName = "glucose";

    // Skalierungsfaktor, da PDB-Koordinaten in Angstrom sind und in Unity sonst kaum sichtbar waeren.
    public float scaleFactor = 1.0f;

    public float atomSize = 0.3f;
    public float bondRadius = 0.08f;

    void Start()
    {
        StartCoroutine(LoadMoleculeFromPubChem(moleculeName));
    }
    IEnumerator LoadMoleculeFromPubChem(string compoundName) {

        MoleculeImporter importer = GetComponent<MoleculeImporter>();
        Debug.Log(importer == null ? "Importer ist NULL!" : "Importer gefunden");
        string url = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/{compoundName}/SDF?record_type=3d";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Fehler beim Laden der PubChem-Datei: {request.error}");
            yield break;
        }
        string sdfContent = request.downloadHandler.text;
        string fileName = $"{compoundName}.sdf";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        
        File.WriteAllText(path, sdfContent);
        List<Atom> atoms = importer.ImportFile(path);

        Debug.Log($"{atoms.Count} Atome geladen aus {fileName}");

        foreach (Atom atom in atoms)
        {
            CreateAtomSphere(atom);
        }

        CreateBonds(atoms);
    }

    void CreateAtomSphere(Atom atom)
    {
        // Eine einfache Kugel pro Atom erzeugen
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Position setzen (PDB nutzt X,Y,Z in Angstrom)
        sphere.transform.position = new Vector3(atom.X, atom.Y, atom.Z) * scaleFactor;

        // Kleine, einheitliche Groesse fuer den ersten Test
        // (spaeter: Radius je nach Element unterschiedlich)
        sphere.transform.localScale = Vector3.one * atomSize;

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

    void CreateBonds(List<Atom> atoms)
    {
        HashSet<Bond> createdBonds = new HashSet<Bond>();

        foreach (Atom atom in atoms)
        {
            foreach (Bond bond in atom.Bonds)
            {
                if (createdBonds.Contains(bond))
                    continue;

                createdBonds.Add(bond);
                CreateBondCylinder(bond);
            }
        }
    }

    void CreateBondCylinder(Bond bond)
    {
        Vector3 pos1 = new Vector3(bond.Atom1.X, bond.Atom1.Y, bond.Atom1.Z) * scaleFactor;
        Vector3 pos2 = new Vector3(bond.Atom2.X, bond.Atom2.Y, bond.Atom2.Z) * scaleFactor;

        Vector3 middle = (pos1 + pos2) / 2f;
        Vector3 direction = pos2 - pos1;
        float length = direction.magnitude;

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        cylinder.transform.position = middle;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(bondRadius, length / 2f, bondRadius);
        cylinder.transform.parent = this.transform;

        cylinder.name = $"Bond_{bond.Atom1.SerialNumber}_{bond.Atom2.SerialNumber}";

        Renderer renderer = cylinder.GetComponent<Renderer>();
        renderer.material.color = GetColorForBond(bond.Type);
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

    Color GetColorForBond(BondType type)
    {
        switch (type)
        {
            case BondType.Single:
                return Color.white;
            case BondType.Double:
                return Color.green;
            case BondType.Triple:
                return Color.cyan;
            case BondType.Aromatic:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
}