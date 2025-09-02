using System;
using UnityEngine;

public static class PoolRecycle
{
    public static void Recycle(GameObject go)
    {
        if (!go) return;
        var bind = go.GetComponent<FactoryBinding>();
        if (bind == null || string.IsNullOrEmpty(bind.typeId)) { go.SetActive(false); return; }
        var t = Type.GetType(bind.typeId);
        if (t == null) { go.SetActive(false); return; }
        var fac = Services.Get(t) as FactoryBase;
        if (fac != null) fac.RecycleGO(go);
        else go.SetActive(false);
    }
}
