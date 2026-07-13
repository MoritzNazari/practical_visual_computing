using System.Collections.Generic;
using UnityEngine;
namespace MoleculeReaderInterface
{
    public interface IMoleculeReader
    {
        bool CanRead(string extension);
        List<Atom> Read(string path);
    }
}
