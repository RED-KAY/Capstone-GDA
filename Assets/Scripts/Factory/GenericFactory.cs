using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class GenericFactory<T> : FactoryBase where T : Component
{
    [SerializeField] T[] prefabs;
    [SerializeField] bool recycle = true;

    List<T>[] pools;
    List<T>[] spawned;
    Scene poolScene;

    public int Count => prefabs != null ? prefabs.Length : 0;

    public T Get(int prefabId = 0)
    {
        if (prefabs == null || prefabs.Length == 0) { Debug.LogError($"{name}: no prefabs set."); return null; }
        prefabId = Mathf.Clamp(prefabId, 0, prefabs.Length - 1);

        if (!recycle)
        {
            var inst = Instantiate(prefabs[prefabId]);
            Bind(inst, prefabId);
            return inst;
        }

        EnsurePools();

        // 1) pooled
        var pool = pools[prefabId];
        int last = pool.Count - 1;
        if (last >= 0)
        {
            var t = pool[last];
            pool.RemoveAt(last);
            var b = t.GetComponent<FactoryBinding>(); if (b) b.inPool = false;
            t.gameObject.SetActive(true);
            return t;
        }

        // 2) harvest disabled from spawned (in case caller forgot Recycle)
        var list = spawned[prefabId];
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i]; if (!t) continue;
            if (!t.gameObject.activeSelf)
            {
                var b = t.GetComponent<FactoryBinding>(); if (b && !b.inPool) { b.inPool = true; pool.Add(t); }
            }
        }
        last = pool.Count - 1;
        if (last >= 0)
        {
            var t = pool[last];
            pool.RemoveAt(last);
            var b = t.GetComponent<FactoryBinding>(); if (b) b.inPool = false;
            t.gameObject.SetActive(true);
            return t;
        }

        // 3) none → make new
        var instNew = Instantiate(prefabs[prefabId]);
        Bind(instNew, prefabId);
        SceneManager.MoveGameObjectToScene(instNew.gameObject, poolScene);
        return instNew;
    }

    public T GetRandom() => (prefabs == null || prefabs.Length == 0) ? null : Get(Random.Range(0, prefabs.Length));

    // ------ NEW: explicit recycle ------
    public void Recycle(T t)
    {
        if (!t) return;
        if (!recycle) { Destroy(t.gameObject); return; }

        EnsurePools();

        var b = t.GetComponent<FactoryBinding>();
        if (!b) b = t.gameObject.AddComponent<FactoryBinding>();

        int id = (b.prefabId >= 0 && b.prefabId < prefabs.Length) ? b.prefabId : 0;

        t.gameObject.SetActive(false);
        if (!spawned[id].Contains(t)) spawned[id].Add(t);

        if (!b.inPool)
        {
            b.inPool = true;
            pools[id].Add(t);
        }
    }

    internal override void RecycleGO(GameObject go)
    {
        var t = go ? go.GetComponent<T>() : null;
        if (t) Recycle(t);
        else if (go) Destroy(go);
    }

    // ---- internals ----
    void Bind(T obj, int prefabId)
    {
        var b = obj.GetComponent<FactoryBinding>();
        if (!b) b = obj.gameObject.AddComponent<FactoryBinding>();
        b.prefabId = prefabId;
        b.typeId = GetType().AssemblyQualifiedName;
        b.inPool = false;

        EnsurePools();
        spawned[prefabId].Add(obj);
    }

    void EnsurePools()
    {
        if (pools != null) return;
        int n = Mathf.Max(1, prefabs.Length);
        pools   = new List<T>[n];
        spawned = new List<T>[n];
        for (int i = 0; i < n; i++) { pools[i] = new List<T>(32); spawned[i] = new List<T>(64); }

        if (Application.isEditor)
        {
            poolScene = SceneManager.GetSceneByName(name);
            if (poolScene.isLoaded) return;
        }
        poolScene = SceneManager.CreateScene(name);
    }
}
