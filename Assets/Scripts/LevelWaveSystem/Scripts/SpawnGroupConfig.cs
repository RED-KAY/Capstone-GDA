using UnityEngine;

namespace WaveSystem.Data
{
    /// <summary>
    /// Represents a group of entities to spawn with specific parameters.
    /// This is entity-agnostic and can spawn any type of GameObject.
    /// </summary>
    [System.Serializable]
    public class SpawnGroupConfig
    {
        [Header("Entity Configuration")]
        [Tooltip("The prefab to spawn - can be any GameObject")]
        [SerializeField] private Enemy entityPrefab;
        
        [Header("Spawn Parameters")]
        [Tooltip("Maximum number of entities to spawn in this group")]
        [SerializeField] private int maxAmount = 10;
        
        [Tooltip("Number of entities to spawn at once")]
        [SerializeField] private int entitiesPerSpawn = 2;
        
        [Tooltip("Time interval in seconds between spawn batches")]
        [SerializeField] private float spawnInterval = 3f;
        
        #region Properties
        
        /// <summary>
        /// The prefab to spawn
        /// </summary>
        public Enemy EntityPrefab => entityPrefab;
        
        /// <summary>
        /// Maximum number of entities to spawn in this group
        /// </summary>
        public int MaxAmount => maxAmount;
        
        /// <summary>
        /// Number of entities to spawn at once
        /// </summary>
        public int EntitiesPerSpawn => entitiesPerSpawn;
        
        /// <summary>
        /// Time interval between spawn batches
        /// </summary>
        public float SpawnInterval => spawnInterval;
        
        /// <summary>
        /// Calculates the total number of spawn batches needed
        /// </summary>
        public int TotalBatches => Mathf.CeilToInt((float)maxAmount / entitiesPerSpawn);
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the spawn group configuration
        /// </summary>
        public bool Validate()
        {
            if (entityPrefab == null)
            {
                Debug.LogError("[SpawnGroupConfig] Entity prefab is null!");
                return false;
            }
            
            if (maxAmount <= 0)
            {
                Debug.LogError("[SpawnGroupConfig] Max amount must be greater than 0!");
                return false;
            }
            
            if (entitiesPerSpawn <= 0)
            {
                Debug.LogError("[SpawnGroupConfig] Entities per spawn must be greater than 0!");
                return false;
            }
            
            if (spawnInterval < 0)
            {
                Debug.LogError("[SpawnGroupConfig] Spawn interval cannot be negative!");
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        /// <summary>
        /// Creates a copy of this spawn group configuration
        /// </summary>
        public SpawnGroupConfig Clone()
        {
            return new SpawnGroupConfig
            {
                entityPrefab = this.entityPrefab,
                maxAmount = this.maxAmount,
                entitiesPerSpawn = this.entitiesPerSpawn,
                spawnInterval = this.spawnInterval
            };
        }
        
        public override string ToString()
        {
            return $"SpawnGroup: {(entityPrefab != null ? entityPrefab.name : "None")} - " +
                   $"Max: {maxAmount}, PerSpawn: {entitiesPerSpawn}, Interval: {spawnInterval}s";
        }
    }
}