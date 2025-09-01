using System.Collections.Generic;
using UnityEngine;
using WaveSystem.Core;

namespace WaveSystem.Spawning
{
    /// <summary>
    /// Handles the actual spawning of entities at specified locations.
    /// This class is entity-agnostic and can spawn any GameObject.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private float spawnHeightOffset = 0.5f;
        [SerializeField] private float minDistanceBetweenSpawns = 1f;
        [SerializeField] private int maxSpawnAttempts = 10;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showSpawnGizmos = true;
        
        // Track spawn positions for debugging
        private List<Vector3> m_RecentSpawnPositions = new List<Vector3>();
        private float m_GizmoDisplayDuration = 2f;

        [SerializeField] private Transform[] m_ZombieTypesParents;

        #region Public Methods

        /// <summary>
        /// Spawns entities at random positions within the specified bounds
        /// </summary>
        /// <param name="entityPrefab">The prefab to spawn</param>
        /// <param name="count">Number of entities to spawn</param>
        /// <param name="spawnBounds">The bounds within which to spawn</param>
        /// <returns>List of spawned GameObjects</returns>
        public List<Enemy> SpawnEntities(Enemy entityPrefab, int count, Bounds spawnBounds, int type)
        {
            var spawnedEntities = new List<Enemy>();

            if (entityPrefab == null)
            {
                Debug.LogError("[Spawner] Cannot spawn null prefab!");
                return spawnedEntities;
            }

            if (count <= 0)
            {
                Debug.LogWarning("[Spawner] Spawn count must be greater than 0!");
                return spawnedEntities;
            }

            // Generate spawn positions
            var spawnPositions = GenerateSpawnPositions(count, spawnBounds);

            // Spawn entities at generated positions
            foreach (var position in spawnPositions)
            {
                Enemy spawnedEntity = Instantiate(entityPrefab, position, Quaternion.identity);
                spawnedEntity.transform.SetParent(m_ZombieTypesParents[type]); // Set parent based on type
                spawnedEntity.transform.position += Vector3.up * spawnHeightOffset; // Apply height offset
                spawnedEntities.Add(spawnedEntity);
                ZombiesManager.Instance.AddZombieToGroup(entityPrefab.m_Type, spawnedEntity);

                LogDebug($"Spawned {entityPrefab.name} at {position}");
            }

            // Track positions for debug visualization
            if (showSpawnGizmos)
            {
                m_RecentSpawnPositions.Clear();
                m_RecentSpawnPositions.AddRange(spawnPositions);
                Invoke(nameof(ClearRecentSpawnPositions), m_GizmoDisplayDuration);
            }

            return spawnedEntities;
        }
        
        /// <summary>
        /// Spawns entities at random positions within specified box collider
        /// </summary>
        /// <param name="entityPrefab">The prefab to spawn</param>
        /// <param name="count">Number of entities to spawn</param>
        /// <param name="spawnArea">Box collider defining the spawn area</param>
        /// <returns>List of spawned GameObjects</returns>
        public List<Enemy> SpawnEntities(Enemy entityPrefab, int count, BoxCollider spawnArea, int type)
        {
            if (spawnArea == null)
            {
                Debug.LogError("[Spawner] Spawn area BoxCollider is null!");
                return new List<Enemy>();
            }
            
            // Convert BoxCollider to bounds in world space
            Bounds worldBounds = new Bounds(
                spawnArea.transform.TransformPoint(spawnArea.center),
                Vector3.Scale(spawnArea.size, spawnArea.transform.lossyScale)
            );
            
            return SpawnEntities(entityPrefab, count, worldBounds, type);
        }
        
        /// <summary>
        /// Spawns a single entity at a specific position
        /// </summary>
        /// <param name="entityPrefab">The prefab to spawn</param>
        /// <param name="position">World position to spawn at</param>
        /// <param name="rotation">Rotation of the spawned entity</param>
        /// <returns>The spawned GameObject</returns>
        public Enemy SpawnEntity(Enemy entityPrefab, Vector3 position, Quaternion rotation)
        {
            if (entityPrefab == null)
            {
                Debug.LogError("[Spawner] Cannot spawn null prefab!");
                return null;
            }
            
            Enemy spawnedEntity = Instantiate(entityPrefab, position, rotation);
            spawnedEntity.transform.SetParent(m_ZombieTypesParents[entityPrefab.m_Type]); 
            ZombiesManager.Instance.AddZombieToGroup(entityPrefab.m_Type, spawnedEntity);
            
            LogDebug($"Spawned {entityPrefab.name} at {position}");
            
            return spawnedEntity;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Generates random spawn positions within bounds
        /// </summary>
        private List<Vector3> GenerateSpawnPositions(int count, Bounds bounds)
        {
            var positions = new List<Vector3>();
            var usedPositions = new HashSet<Vector3>();
            
            for (int i = 0; i < count; i++)
            {
                Vector3 position = Vector3.zero;
                bool validPositionFound = false;
                
                // Try to find a valid position
                for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
                {
                    // Generate random position within bounds
                    position = new Vector3(
                        Random.Range(bounds.min.x, bounds.max.x),
                        0f,
                        Random.Range(bounds.min.z, bounds.max.z)
                    );
                    
                    // Check if position is far enough from other spawn points
                    if (IsPositionValid(position, usedPositions))
                    {
                        validPositionFound = true;
                        break;
                    }
                }
                
                if (validPositionFound)
                {
                    positions.Add(position);
                    usedPositions.Add(position);
                }
                else
                {
                    // Fallback: use the last generated position even if too close
                    positions.Add(position);
                    LogDebug($"Warning: Could not find valid spawn position after {maxSpawnAttempts} attempts");
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Checks if a position is valid (far enough from other positions)
        /// </summary>
        private bool IsPositionValid(Vector3 position, HashSet<Vector3> usedPositions)
        {
            foreach (var usedPos in usedPositions)
            {
                if (Vector3.Distance(position, usedPos) < minDistanceBetweenSpawns)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Clears recent spawn positions (for debug visualization)
        /// </summary>
        private void ClearRecentSpawnPositions()
        {
            m_RecentSpawnPositions.Clear();
        }
        
        /// <summary>
        /// Logs debug messages if enabled
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Spawner] {message}");
            }
        }
        
        #endregion
        
        #region Debug Visualization
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showSpawnGizmos) return;
            
            // Draw recent spawn positions
            Gizmos.color = Color.cyan;
            foreach (var pos in m_RecentSpawnPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.5f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 2f);
            }
        }
#endif
        
        #endregion
    }
}