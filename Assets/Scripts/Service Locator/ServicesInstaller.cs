using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ServicesInstaller : MonoBehaviour
{
    public ScriptableObject[] assets; // e.g. EnemyFactory, BulletFactory, FxFactory

    void Awake()
    {
        if (assets == null) return;
        for (int i = 0; i < assets.Length; i++)
        {
            var a = assets[i];
            if (!a) continue;
            Services.Add(a.GetType(), a);
        }
    }
}
