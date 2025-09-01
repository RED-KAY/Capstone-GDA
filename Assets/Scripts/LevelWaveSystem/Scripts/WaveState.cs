using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaveSystem.Data;

namespace WaveSystem.Core
{
    /// <summary>
    /// Tracks the runtime state of a wave during execution
    /// </summary>
    public class WaveState
    {
        private WaveConfig m_WaveConfig;
        private Dictionary<SpawnGroupConfig, SpawnGroupState> m_GroupStates;
        private List<Enemy> m_SpawnedEntities;
        private int m_CurrentGroupIndex = 0;
        private List<(SpawnGroupConfig group, int count)> m_ReadyGroupsCache = new();
        #region Properties
        
        /// <summary>
        /// The configuration for this wave
        /// </summary>
        public WaveConfig Config => m_WaveConfig;
        
        /// <summary>
        /// Current status of the wave
        /// </summary>
        public WaveStatus Status { get; private set; }
        
        /// <summary>
        /// Total number of entities spawned
        /// </summary>
        public int TotalSpawned { get; private set; }
        
        /// <summary>
        /// Total number of entities still alive
        /// </summary>
        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = m_SpawnedEntities.Count - 1; i >= 0; i--)
                {
                    if (m_SpawnedEntities[i] == null)
                    {
                        m_SpawnedEntities.RemoveAt(i);
                    }
                    else
                    {
                        count++;
                    }
                }
                return count;
            }
        }
        
        /// <summary>
        /// Check if all entities have been spawned
        /// </summary>
        public bool AllEntitiesSpawned
        {
            get
            {
                foreach (var groupState in m_GroupStates.Values)
                {
                    if (!groupState.IsComplete)
                        return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// Check if all spawned entities are dead
        /// </summary>
        public bool AreAllEntitiesDead => AliveCount == 0 && AllEntitiesSpawned;
        
        /// <summary>
        /// List of all spawned entities
        /// </summary>
        public IReadOnlyList<Enemy> SpawnedEntities => m_SpawnedEntities;
        
        #endregion
        
        #region Constructor
        
        public WaveState(WaveConfig waveConfig)
        {
            m_WaveConfig = waveConfig;
            m_GroupStates = new Dictionary<SpawnGroupConfig, SpawnGroupState>();
            m_SpawnedEntities = new List<Enemy>();
            Status = WaveStatus.Waiting;
            TotalSpawned = 0;
            
            // Initialize group states
            foreach (var group in waveConfig.SpawnGroups)
            {
                if (group != null)
                {
                    m_GroupStates[group] = new SpawnGroupState(group);
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts the wave
        /// </summary>
        public void Start()
        {
            if (Status != WaveStatus.Waiting)
            {
                Debug.LogWarning($"[WaveState] Cannot start wave in {Status} status");
                return;
            }
            
            Status = WaveStatus.InProgress;
            Debug.Log($"[WaveState] Wave '{m_WaveConfig.WaveName}' started");
        }
        
        /// <summary>
        /// Gets spawn groups that are ready to spawn
        /// </summary>
        public void GetGroupsReadyToSpawnParallel()
        {
            foreach (var kvp in m_GroupStates)
            {
                var group = kvp.Key;
                var state = kvp.Value;
                
                if (state.IsReadyToSpawn())
                {
                    int spawnCount = Mathf.Min(group.EntitiesPerSpawn, state.Remaining);
                    m_ReadyGroupsCache.Add((group, spawnCount));
                }
            }
        }
        
        private void GetGroupsReadyToSpawnSequential()
        {
            if (m_CurrentGroupIndex >= m_WaveConfig.SpawnGroups.Count)
                return ;
    
            var currentGroup = m_WaveConfig.SpawnGroups[m_CurrentGroupIndex];
            if (currentGroup == null) 
            {
                m_CurrentGroupIndex++;
                return;
            }
    
            var groupState = m_GroupStates[currentGroup];
    
            // Move to next group if current is complete
            if (groupState.IsComplete)
            {
                m_CurrentGroupIndex++;
                return;
            }
    
            // Check if ready to spawn
            if (groupState.IsReadyToSpawn())
            {
                int spawnCount = Mathf.Min(currentGroup.EntitiesPerSpawn, groupState.Remaining);
                m_ReadyGroupsCache.Add((currentGroup, spawnCount));
            }
        }
        
        /// <summary>
        /// Records spawned entities for a group
        /// </summary>
        public void RecordSpawn(SpawnGroupConfig group, List<Enemy> spawnedEntities)
        {
            if (!m_GroupStates.ContainsKey(group))
            {
                Debug.LogError($"[WaveState] Unknown spawn group!");
                return;
            }
            
            var state = m_GroupStates[group];
            state.RecordSpawn(spawnedEntities.Count);
            
            m_SpawnedEntities.AddRange(spawnedEntities);
            TotalSpawned += spawnedEntities.Count;
            
            // Check if spawning is complete
            if (AllEntitiesSpawned && Status == WaveStatus.InProgress)
            {
                Status = WaveStatus.SpawnComplete;
                Debug.Log($"[WaveState] Wave '{m_WaveConfig.WaveName}' spawn complete - {TotalSpawned} entities spawned");
            }
        }
        
        /// <summary>
        /// Updates the wave state
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Status != WaveStatus.InProgress && Status != WaveStatus.SpawnComplete)
                return;

            // Update group timers based on spawning mode
            if (m_WaveConfig.SequentialGroupSpawning)
            {
                // In sequential mode, only update the current group's timer
                if (m_CurrentGroupIndex < m_WaveConfig.SpawnGroups.Count)
                {
                    var currentGroup = m_WaveConfig.SpawnGroups[m_CurrentGroupIndex];
                    if (currentGroup != null && m_GroupStates.ContainsKey(currentGroup))
                    {
                        m_GroupStates[currentGroup].UpdateTimer(deltaTime);
                    }
                }
                
            }
            else
            {
                // In parallel mode, Update all group timers.
                foreach (var state in m_GroupStates.Values)
                {
                    state.UpdateTimer(deltaTime);
                }
            }
            
            // Check if wave is complete (all dead)
            if (Status == WaveStatus.SpawnComplete && AreAllEntitiesDead)
            {
                Complete();
            }
        }
        
        /// <summary>
        /// Forces the wave to complete
        /// </summary>
        public void Complete()
        {
            if (Status == WaveStatus.Complete)
                return;
            
            Status = WaveStatus.Complete;
            Debug.Log($"[WaveState] Wave '{m_WaveConfig.WaveName}' completed.");
        }
        
        /// <summary>
        /// Cleans up the wave state
        /// </summary>
        public void Cleanup()
        {
            // Destroy any remaining entities
            foreach (var entity in m_SpawnedEntities)
            {
                if (entity != null)
                {
                    Object.Destroy(entity);
                }
            }
            
            m_SpawnedEntities.Clear();
            m_GroupStates.Clear();
            Status = WaveStatus.Complete;
        }
        
        #endregion
        
        /// <summary>
        /// Gets debug information about the wave state
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Wave: {m_WaveConfig.WaveName}\n" +
                   $"Status: {Status}\n" +
                   $"Spawned: {TotalSpawned}/{m_WaveConfig.TotalEntities}\n" +
                   $"Alive: {AliveCount}";
        }

        public List<(SpawnGroupConfig group, int count)> GetGroupsReadyToSpawn()
        {
            m_ReadyGroupsCache.Clear();
            
            if (Status != WaveStatus.InProgress)
                return m_ReadyGroupsCache;
            
            if (m_WaveConfig.SequentialGroupSpawning)
            {
                GetGroupsReadyToSpawnSequential();
            }
            else
            {
                GetGroupsReadyToSpawnParallel();
            }

            return m_ReadyGroupsCache;
        }
    }
    
    /// <summary>
    /// Tracks the state of a spawn group within a wave
    /// </summary>
    internal class SpawnGroupState
    {
        private SpawnGroupConfig m_Config;
        private float m_TimeSinceLastSpawn;
        
        public int Spawned { get; private set; }
        public int Remaining => m_Config.MaxAmount - Spawned;
        public bool IsComplete => Spawned >= m_Config.MaxAmount;
        
        public SpawnGroupState(SpawnGroupConfig config)
        {
            m_Config = config;
            Spawned = 0;
            m_TimeSinceLastSpawn = 0f;
        }
        
        public bool IsReadyToSpawn()
        {
            return !IsComplete && m_TimeSinceLastSpawn >= m_Config.SpawnInterval;
        }
        
        public void RecordSpawn(int count)
        {
            Spawned += count;
            m_TimeSinceLastSpawn = 0f;
        }
        
        public void UpdateTimer(float deltaTime)
        {
            if (!IsComplete)
            {
                m_TimeSinceLastSpawn += deltaTime;
            }
        }
    }
    
    /// <summary>
    /// Status of a wave
    /// </summary>
    public enum WaveStatus
    {
        Waiting,        // Wave hasn't started yet
        InProgress,     // Wave is actively spawning
        SpawnComplete,  // All entities spawned, waiting for them to die
        Complete        // All entities dead, wave complete
    }
}