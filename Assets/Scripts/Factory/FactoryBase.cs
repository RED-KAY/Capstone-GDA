using UnityEngine;

public abstract class FactoryBase : ScriptableObject
{
    internal abstract void RecycleGO(GameObject go);
}