using UnityEngine;
using MoleculeReaderInterface;
using System.Collections.Generic;
using System.IO;
using System.Globalization;


public class PDBReader : IMoleculeReader
{
    public bool CanRead(string extension) => extension == ".pdb";
    public List<Atom> Read(string path) { 
    
    // Liest eine lokale PDB-Datei und gibt eine Liste der geparsten Atome zurück.

        var atoms = new List<Atom>();
        var lines = File.ReadAllLines(path);

        foreach (var line in lines)
        {
            // Nur ATOM-Zeilen für den ersten Entwurf.
            if (line.Length < 6) continue;
            string recordType = line.Substring(0, 6).Trim();

            if (recordType != "ATOM") continue;

            // Zeile muss mindestens bis Spalte 54 (Z-Koordinate) reichen
            if (line.Length < 54) continue;

            try
            {
                var atom = new Atom
                {
                    SerialNumber = ParseIntSafe(line.Substring(6, 5)),
                    AtomName = line.Substring(12, 4).Trim(),
                    ResidueName = line.Substring(17, 3).Trim(),
                    ChainId = line.Length > 21 ? line[21] : ' ',
                    ResidueSequenceNumber = ParseIntSafe(line.Substring(22, 4)),
                    X = ParseFloatSafe(line.Substring(30, 8)),
                    Y = ParseFloatSafe(line.Substring(38, 8)),
                    Z = ParseFloatSafe(line.Substring(46, 8)),
                };

                // Element steht ab Spalte 77-78, ist aber nicht immer vorhanden/zuverlässig.
                // Fallback: aus dem Atomnamen raten (erstes alphabetisches Zeichen).
                if (line.Length >= 78)
                {
                    atom.Element = line.Substring(76, 2).Trim();
                }
                if (string.IsNullOrEmpty(atom.Element))
                {
                    atom.Element = GuessElementFromAtomName(atom.AtomName);
                }

                atoms.Add(atom);
            }
            catch (System.Exception ex)
            {
                // Für den ersten Entwurf: fehlerhafte Zeilen einfach überspringen
                // und loggen, statt das ganze Parsing abzubrechen.
                System.Console.WriteLine($"Konnte Zeile nicht parsen: {line}\n{ex.Message}");
            }
        }

        return atoms;
    }

    private static int ParseIntSafe(string s)
    {
        return int.TryParse(s.Trim(), out int result) ? result : 0;
    }

    private static float ParseFloatSafe(string s)
    {
        return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : 0f;
    }

    // Sehr einfache Heuristik: nimm das erste alphabetische Zeichen des Atomnamens.
    // Reicht fuer Standardfaelle (CA -> C, N -> N, O -> O). Fuer Edge Cases
    // (z.B. zweistellige Elemente wie FE, ZN) muesste man das spaeter verfeinern.
    private static string GuessElementFromAtomName(string atomName)
    {
        foreach (char c in atomName)
        {
            if (char.IsLetter(c))
            {
                return c.ToString().ToUpper();
            }
        }
        return "X"; // Unbekannt
    }

}

public class PDBxReader : IMoleculeReader
{
    public bool CanRead(string extension) => extension is ".cif" or ".pdbx";
    public List<Atom> Read(string path) { /* PDBx-Parsing */ return new List<Atom>(); }
}