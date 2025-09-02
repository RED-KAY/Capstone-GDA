using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using WaveSystem.Core;

namespace WaveSystem.Spawning
{
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private float spawnHeightOffset = 0.5f;
        [SerializeField] private float minDistanceBetweenSpawns = 1f;
        [SerializeField] private int maxSpawnAttempts = 10;

        [Header("NavMesh")]
        [SerializeField] private bool snapToNavMesh = true;
        [SerializeField] private float navSampleRadius = 2f;
        [SerializeField] private int navAreaMask = NavMesh.AllAreas;

        [Header("Parents Per Type")]
        [SerializeField] private Transform[] m_ZombieTypesParents;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showSpawnGizmos = true;

        readonly List<Vector3> m_RecentSpawnPositions = new List<Vector3>();
        float m_GizmoDisplayDuration = 2f;

        public List<Enemy> SpawnEntities(Enemy prefab, int count, Bounds spawnBounds, int type)
        {
            var outList = new List<Enemy>();
            if (!prefab) { Debug.LogError("[Spawner] prefab null"); return outList; }
            if (count <= 0) { Log("count <= 0"); return outList; }

            var poses = GenerateSpawnPositions(count, spawnBounds);

            foreach (var basePos in poses)
            {
                Vector3 p = basePos;
                if (snapToNavMesh && NavMesh.SamplePosition(p, out var hit, navSampleRadius, navAreaMask))
                    p = hit.position;

                p += Vector3.up * spawnHeightOffset;

                var z = Instantiate(prefab, p, Quaternion.identity);
                SetParentByType(z.transform, type);

                // register in your manager
                ZombiesManager.Instance.AddZombieToGroup(type, z);

                outList.Add(z);
                Log($"Spawned {prefab.name} type={type} at {p}");
            }

            TrackGizmos(poses);
            return outList;
        }

        public List<Enemy> SpawnEntities(Enemy prefab, int count, BoxCollider spawnArea, int type)
        {
            if (!spawnArea)
            {
                Debug.LogError("[Spawner] BoxCollider null");
                return new List<Enemy>();
            }

            Bounds worldBounds = new Bounds(
                spawnArea.transform.TransformPoint(spawnArea.center),
                Vector3.Scale(spawnArea.size, spawnArea.transform.lossyScale)
            );

            return SpawnEntities(prefab, count, worldBounds, type);
        }

        public Enemy SpawnEntity(Enemy prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab) { Debug.LogError("[Spawner] prefab null"); return null; }

            Vector3 p = position;
            if (snapToNavMesh && NavMesh.SamplePosition(p, out var hit, navSampleRadius, navAreaMask))
                p = hit.position;

            p += Vector3.up * spawnHeightOffset;

            var z = Instantiate(prefab, p, rotation);
            SetParentByType(z.transform, prefab.m_Type);
            ZombiesManager.Instance.AddZombieToGroup(prefab.m_Type, z);
            Log($"Spawned {prefab.name} type={prefab.m_Type} at {p}");
            return z;
        }

        // --- helpers ---
        void SetParentByType(Transform t, int type)
        {
            if (m_ZombieTypesParents == null || type < 0 || type >= m_ZombieTypesParents.Length) return;
            var parent = m_ZombieTypesParents[type];
            if (parent) t.SetParent(parent, true);
        }

        List<Vector3> GenerateSpawnPositions(int count, Bounds bounds)
        {
            var list = new List<Vector3>(count);
            var used = new List<Vector3>(count);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = Vector3.zero;
                bool ok = false;

                for (int a = 0; a < maxSpawnAttempts; a++)
                {
                    pos = new Vector3(
                        Random.Range(bounds.min.x, bounds.max.x),
                        0f,
                        Random.Range(bounds.min.z, bounds.max.z)
                    );

                    if (IsValid(pos, used)) { ok = true; break; }
                }

                list.Add(pos);
                if (!ok) Log($"fallback pos used after {maxSpawnAttempts} tries");
                used.Add(pos);
            }
            return list;
        }

        bool IsValid(Vector3 p, List<Vector3> used)
        {
            for (int i = 0; i < used.Count; i++)
                if (Vector3.Distance(p, used[i]) < minDistanceBetweenSpawns) return false;
            return true;
        }

        void TrackGizmos(List<Vector3> positions)
        {
            if (!showSpawnGizmos) return;
            m_RecentSpawnPositions.Clear();
            m_RecentSpawnPositions.AddRange(positions);
            CancelInvoke(nameof(ClearRecentSpawnPositions));
            Invoke(nameof(ClearRecentSpawnPositions), m_GizmoDisplayDuration);
        }

        void ClearRecentSpawnPositions() => m_RecentSpawnPositions.Clear();

        void Log(string m) { if (enableDebugLogs) Debug.Log($"[Spawner] {m}"); }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showSpawnGizmos) return;
            Gizmos.color = Color.cyan;
            foreach (var pos in m_RecentSpawnPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.5f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 2f);
            }
        }
#endif
    }
}
