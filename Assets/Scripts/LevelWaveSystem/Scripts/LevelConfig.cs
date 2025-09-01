using System.Collections.Generic;
using UnityEngine;
using WaveSystem.Core;

namespace WaveSystem.Data
{
    /// <summary>
    /// Configuration for a complete level containing multiple waves
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "TowerDefense/WaveSystem/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Settings")]
        [SerializeField] private string levelName = "Level";
        
        [Header("Spawn Directions")]
        [Tooltip("Directions from which entities will spawn")]
        [SerializeField] private SpawnDirection spawnDirections = SpawnDirection.North | SpawnDirection.South;
        
        [Header("Wave Settings")]
        [Tooltip("Time in seconds between wave completion and next wave start")]
        [SerializeField] private float intervalBetweenWaves = 10f;
        
        [Tooltip("List of waves in this level")]
        [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
        
        #region Properties
        
        /// <summary>
        /// Name identifier for this level
        /// </summary>
        public string LevelName => levelName;
        
        /// <summary>
        /// Active spawn directions for this level
        /// </summary>
        public SpawnDirection SpawnDirections => spawnDirections;
        
        /// <summary>
        /// Time interval between waves
        /// </summary>
        public float IntervalBetweenWaves => intervalBetweenWaves;
        
        /// <summary>
        /// List of waves in this level
        /// </summary>
        public IReadOnlyList<WaveConfig> Waves => waves;
        
        /// <summary>
        /// Total number of waves in this level
        /// </summary>
        public int WaveCount => waves.Count;
        
        /// <summary>
        /// Total number of entities to spawn in the entire level
        /// </summary>
        public int TotalEntities
        {
            get
            {
                int total = 0;
                foreach (var wave in waves)
                {
                    if (wave != null)
                        total += wave.TotalEntities;
                }
                return total;
            }
        }
        
        /// <summary>
        /// Number of active spawn directions
        /// </summary>
        public int ActiveDirectionCount => spawnDirections.GetActiveDirectionCount();
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the level configuration
        /// </summary>
        public bool Validate()
        {
            if (spawnDirections == SpawnDirection.None)
            {
                Debug.LogError($"[LevelConfig] {levelName} has no spawn directions!");
                return false;
            }
            
            if (waves == null || waves.Count == 0)
            {
                Debug.LogError($"[LevelConfig] {levelName} has no waves!");
                return false;
            }
            
            if (intervalBetweenWaves < 0)
            {
                Debug.LogError($"[LevelConfig] {levelName} has negative interval between waves!");
                return false;
            }
            
            bool isValid = true;
            for (int i = 0; i < waves.Count; i++)
            {
                if (waves[i] == null)
                {
                    Debug.LogError($"[LevelConfig] {levelName} has null wave at index {i}!");
                    isValid = false;
                }
                else if (!waves[i].Validate())
                {
                    Debug.LogError($"[LevelConfig] {levelName} has invalid wave at index {i}!");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Gets a wave by index
        /// </summary>
        public WaveConfig GetWave(int index)
        {
            if (index >= 0 && index < waves.Count)
                return waves[index];
            return null;
        }
        
        /// <summary>
        /// Calculates how many entities should spawn at each direction
        /// </summary>
        public Dictionary<SpawnDirection, int> DistributeEntities(int totalEntities)
        {
            var distribution = new Dictionary<SpawnDirection, int>();
            var directions = spawnDirections.GetIndividualDirections();
            int directionCount = directions.Length;
            
            if (directionCount == 0) return distribution;
            
            // Initialize all direction's distribution with 0.
            foreach (var dir in directions)
            {
                distribution[dir] = 0;
            }
            
            // Base amount per direction
            int baseAmount = totalEntities / directionCount;
            int remainder = totalEntities % directionCount;
            
            // Distribute base amount
            foreach (var dir in directions)
            {
                distribution[dir] = baseAmount;
            }
            
            // Distribute remainder based on precedence (N, W, S, E)
            int index = 0;
            while (remainder > 0 && index < directions.Length)
            {
                distribution[directions[index]]++;
                remainder--;
                index++;
            }
            
            return distribution;
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = name;
            }
            
            // Remove null waves
            waves?.RemoveAll(wave => wave == null);
            
            // Ensure at least one direction is selected
            if (spawnDirections == SpawnDirection.None)
            {
                spawnDirections = SpawnDirection.North;
            }
        }
#endif
        
        #endregion
        
        public override string ToString()
        {
            return $"Level: {levelName} - Waves: {WaveCount}, Directions: {spawnDirections}, Total Entities: {TotalEntities}";
        }
    }
}