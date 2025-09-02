using UnityEngine;

public class FactoryBinding : MonoBehaviour
{
    public string typeId;   // factory type (AssemblyQualifiedName)
    public int prefabId = -1;
    public bool inPool = false;
}
