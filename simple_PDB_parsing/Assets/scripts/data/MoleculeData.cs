using System.Collections.Generic;

public class Atom
{
    public int SerialNumber;
    public string AtomName;
    public string ResidueName;
    public char ChainId;
    public int ResidueSequenceNumber;
    public float X, Y, Z;
    public string Element;
    public List<Bond> Bonds { get; set; } = new List<Bond>();


    public override string ToString()
    {
        return $"Atom #{SerialNumber} ({AtomName}) in {ResidueName}{ResidueSequenceNumber} " +
               $"Chain {ChainId} @ ({X:F3}, {Y:F3}, {Z:F3}) Element={Element}";
    }
}

public enum BondType
{
    Single = 1,
    Double = 2,
    Triple = 3,
    Aromatic = 4,          // kein offizieller MDL-Standardwert, aber gaengig (z.B. RDKit-Output)
    SingleOrDouble = 5,
    SingleOrAromatic = 6,
    DoubleOrAromatic = 7,
    Any = 8
}

public class Bond
{
    public Atom Atom1 { get; set; }
    public Atom Atom2 { get; set; }
    public BondType Type { get; set; }
    public int Stereo { get; set; } // optional, 0 = keine Angabe

    public Atom GetOther(Atom atom) => atom == Atom1 ? Atom2 : Atom1;
}
