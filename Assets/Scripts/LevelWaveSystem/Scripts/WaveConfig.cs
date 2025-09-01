using System.Collections.Generic;
using UnityEngine;

namespace WaveSystem.Data
{
    /// <summary>
    /// Configuration for a single wave containing multiple spawn groups
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "TowerDefense/WaveSystem/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Wave Settings")]
        [SerializeField] private string waveName = "Wave";
        
        [Tooltip("If true, groups spawn sequentially. If false, groups spawn in parallel.")]
        [SerializeField] private bool sequentialGroupSpawning = false;
        
        [Header("Spawn Groups")]
        [Tooltip("List of different entity types to spawn in this wave")]
        [SerializeField] private List<SpawnGroupConfig> spawnGroups = new List<SpawnGroupConfig>();
        
        #region Properties
        
        /// <summary>
        /// Name identifier for this wave
        /// </summary>
        public string WaveName => waveName;
        
        public bool SequentialGroupSpawning => sequentialGroupSpawning;
        
        /// <summary>
        /// List of spawn groups in this wave
        /// </summary>
        public IReadOnlyList<SpawnGroupConfig> SpawnGroups => spawnGroups;
        
        /// <summary>
        /// Total number of entities to spawn in this wave
        /// </summary>
        public int TotalEntities
        {
            get
            {
                int total = 0;
                foreach (var group in spawnGroups)
                {
                    if (group != null)
                        total += group.MaxAmount;
                }
                return total;
            }
        }
        
        /// <summary>
        /// Estimated duration of the wave (based on spawn intervals)
        /// </summary>
        public float EstimatedDuration
        {
            get
            {
                float maxDuration = 0f;
                foreach (var group in spawnGroups)
                {
                    if (group != null)
                    {
                        float groupDuration = group.TotalBatches * group.SpawnInterval;
                        maxDuration = Mathf.Max(maxDuration, groupDuration);
                    }
                }
                return maxDuration;
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the wave configuration
        /// </summary>
        public bool Validate()
        {
            if (spawnGroups == null || spawnGroups.Count == 0)
            {
                Debug.LogError($"[WaveConfig] {waveName} has no spawn groups!");
                return false;
            }
            
            bool isValid = true;
            for (int i = 0; i < spawnGroups.Count; i++)
            {
                if (spawnGroups[i] == null)
                {
                    Debug.LogError($"[WaveConfig] {waveName} has null spawn group at index {i}!");
                    isValid = false;
                }
                else if (!spawnGroups[i].Validate())
                {
                    Debug.LogError($"[WaveConfig] {waveName} has invalid spawn group at index {i}!");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(waveName))
            {
                waveName = name;
            }
            
            // Remove null entries
            spawnGroups?.RemoveAll(group => group == null);
        }
#endif
        
        #endregion
        
        /// <summary>
        /// Creates a runtime copy of this wave configuration
        /// </summary>
        public WaveConfig CreateRuntimeCopy()
        {
            var copy = CreateInstance<WaveConfig>();
            copy.waveName = this.waveName;
            copy.spawnGroups = new List<SpawnGroupConfig>();
            
            foreach (var group in this.spawnGroups)
            {
                if (group != null)
                    copy.spawnGroups.Add(group.Clone());
            }
            
            return copy;
        }
        
        public override string ToString()
        {
            return $"Wave: {waveName} - Groups: {spawnGroups.Count}, Total Entities: {TotalEntities}";
        }
    }
}