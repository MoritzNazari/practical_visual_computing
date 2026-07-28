using UnityEngine;
using MoleculeReaderInterface;
using System.Collections.Generic;

public class MoleculeImporter : MonoBehaviour
{
    private MoleculeLoader _loader;

    void Awake()
    {
        // SCHRITT 1: Hier, GENAU HIER, werden die konkreten Objekte erzeugt
        var pdbReader = new PDBReader();     // <- Instanziierung #1
        var pdbxReader = new PDBxReader();   // <- Instanziierung #2

        // SCHRITT 2: Beide werden in ein Array gepackt
        IMoleculeReader[] readers = new IMoleculeReader[] { pdbReader, pdbxReader };

        // SCHRITT 3: Das Array wird dem MoleculeLoader-Konstruktor übergeben
        _loader = new MoleculeLoader(readers);

    }

    public List<Atom> ImportFile(string path)
    {
        // SCHRITT 4: Der MoleculeLoader wird benutzt, um die Datei zu laden
        return _loader.Load(path);
    }

}