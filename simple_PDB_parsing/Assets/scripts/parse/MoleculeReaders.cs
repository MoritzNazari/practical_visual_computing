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

public class SDFReader : IMoleculeReader
{
    public bool CanRead(string extension) => extension == ".sdf";

    public List<Atom> Read(string path)
    {
        // Liest eine lokale SDF-Datei (MDL Molfile V2000) und gibt die geparsten Atome
        // inkl. ihrer Bonds zurueck.
        // Hinweis: SDF-Dateien koennen mehrere Molekuele enthalten (getrennt durch "$$$$").
        // Fuer den ersten Entwurf wird nur das erste Molekuel geparst - analog dazu, dass
        // der PDB-Reader bisher auch nur ATOM-Zeilen behandelt.

        var atoms = new List<Atom>();
        var lines = File.ReadAllLines(path);

        if (lines.Length < 4)
        {
            System.Console.WriteLine($"SDF-Datei zu kurz oder ungueltig: {path}");
            return atoms;
        }

        string countsLine = lines[3];
        if (countsLine.Length < 6)
        {
            System.Console.WriteLine($"Counts-Line ungueltig: {countsLine}");
            return atoms;
        }

        int atomCount = ParseIntSafe(countsLine.Substring(0, 3));
        int bondCount = ParseIntSafe(countsLine.Substring(3, 3));

        int atomBlockStart = 4;
        int bondBlockStart = atomBlockStart + atomCount;

        // --- Atom-Block ---
        for (int i = 0; i < atomCount; i++)
        {
            int lineIndex = atomBlockStart + i;
            if (lineIndex >= lines.Length) break;
            string line = lines[lineIndex];

            if (line.Length < 34)
            {
                System.Console.WriteLine($"Konnte Atom-Zeile nicht parsen (zu kurz): {line}");
                continue;
            }

            try
            {
                var atom = new Atom
                {
                    SerialNumber = i + 1, // SDF-Index ist 1-basiert und ergibt sich aus der Position im Block
                    X = ParseFloatSafe(line.Substring(0, 10)),
                    Y = ParseFloatSafe(line.Substring(10, 10)),
                    Z = ParseFloatSafe(line.Substring(20, 10)),
                    Element = line.Substring(31, 3).Trim(),
                };

                // SDF kennt kein AtomName/ResidueName/ChainId wie PDB.
                // AtomName wird hier der Einfachheit halber auf das Element gesetzt.
                atom.AtomName = atom.Element;

                atoms.Add(atom);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Konnte Atom-Zeile nicht parsen: {line}\n{ex.Message}");
            }
        }

        // --- Bond-Block ---
        for (int i = 0; i < bondCount; i++)
        {
            int lineIndex = bondBlockStart + i;
            if (lineIndex >= lines.Length) break;
            string line = lines[lineIndex];

            if (line.Length < 9)
            {
                System.Console.WriteLine($"Konnte Bond-Zeile nicht parsen (zu kurz): {line}");
                continue;
            }

            try
            {
                int atom1Index = ParseIntSafe(line.Substring(0, 3)); // 1-basiert
                int atom2Index = ParseIntSafe(line.Substring(3, 3)); // 1-basiert
                int bondTypeRaw = ParseIntSafe(line.Substring(6, 3));

                if (atom1Index < 1 || atom1Index > atoms.Count ||
                    atom2Index < 1 || atom2Index > atoms.Count)
                {
                    System.Console.WriteLine($"Bond-Zeile referenziert ungueltigen Atomindex: {line}");
                    continue;
                }

                Atom atom1 = atoms[atom1Index - 1];
                Atom atom2 = atoms[atom2Index - 1];

                var bond = new Bond
                {
                    Atom1 = atom1,
                    Atom2 = atom2,
                    Type = (BondType)bondTypeRaw,
                };

                // Ein Bond-Objekt wird bei beiden beteiligten Atomen eingetragen,
                // damit man von jedem Atom aus direkt auf seine Nachbarn zugreifen kann.
                atom1.Bonds.Add(bond);
                atom2.Bonds.Add(bond);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Konnte Bond-Zeile nicht parsen: {line}\n{ex.Message}");
            }
        }

        return atoms;
    }

    private static int ParseIntSafe(string s) =>
        int.TryParse(s.Trim(), out int result) ? result : 0;

    private static float ParseFloatSafe(string s) =>
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : 0f;
}
