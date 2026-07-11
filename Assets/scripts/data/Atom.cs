
public class Atom
{
    public int SerialNumber;
    public string AtomName;
    public string ResidueName;
    public char ChainId;
    public int ResidueSequenceNumber;
    public float X, Y, Z;
    public string Element;

    public override string ToString()
    {
        return $"Atom #{SerialNumber} ({AtomName}) in {ResidueName}{ResidueSequenceNumber} " +
               $"Chain {ChainId} @ ({X:F3}, {Y:F3}, {Z:F3}) Element={Element}";
    }
}
