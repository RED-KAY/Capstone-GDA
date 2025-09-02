using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WaveSystem.Core;
using WaveSystem.Data;
using WaveSystem.Events;
using WaveSystem.Spawning;

namespace WaveSystem
{
    /// <summary>
    /// Main manager that orchestrates the wave spawning system.
    /// Handles level progression, wave management, and entity spawning.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LevelConfig currentLevel;
        
        [SerializeField] private LevelConfig[] m_LevelConfigs;

        public int m_CurrentLevelIndex = 0;

        [Header("Spawn Points")]
        [Tooltip("List of spawn point references for each direction")]
        [SerializeField] private List<DirectionSpawnPoint> spawnPoints = new List<DirectionSpawnPoint>();
        
        [Header("Components")]
        [SerializeField] private Spawner spawner;
        [SerializeField] private TextMeshProUGUI waveCountTxt;

        
        [Header("Settings")]
        [SerializeField] private bool autoStartLevel = false;
        [SerializeField] private bool startNextWaveOnSpawnComplete = false;
        [SerializeField] private float statisticsUpdateInterval = 1f;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugUI = true;
        
        // Runtime state
        private WaveSystemState m_SystemState = WaveSystemState.Idle;
        private int m_CurrentWaveIndex = -1;
        private WaveState m_CurrentWaveState;
        private float m_TimeBetweenWaves;
        private float m_LevelStartTime;
        private Coroutine m_WaveCoroutine;
        private Dictionary<SpawnDirection, DirectionSpawnPoint> m_DirectionLookup;
        
        // Statistics
        private int m_TotalEntitiesKilled;
        private float m_LastStatisticsUpdate;
        
        #region Properties
        
        /// <summary>
        /// Current system state
        /// </summary>
        public WaveSystemState SystemState => m_SystemState;
        
        /// <summary>
        /// Current wave index (0-based)
        /// </summary>
        public int CurrentWaveIndex => m_CurrentWaveIndex;
        
        /// <summary>
        /// Current level configuration
        /// </summary>
        public LevelConfig CurrentLevel => currentLevel;
        
        /// <summary>
        /// Is a level currently active
        /// </summary>
        public bool IsLevelActive => m_SystemState != WaveSystemState.Idle && 
                                     m_SystemState != WaveSystemState.LevelComplete && 
                                     m_SystemState != WaveSystemState.LevelFailed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            currentLevel = m_LevelConfigs[m_CurrentLevelIndex];
            SetWaveCount(1);
            InitializeComponents();
        }
        
        private void Start()
        {
            if (autoStartLevel && currentLevel != null)
            {

                StartLevel(currentLevel);
            }
        }
        
        private void Update()
        {
            UpdateStatistics();
            
            // Update current wave if active
            if (m_CurrentWaveState != null)
            {
                m_CurrentWaveState.Update(Time.deltaTime);
            }
        }
        
        private void OnDestroy()
        {
            StopLevel();
            WaveSystemEvents.ClearAllSubscriptions();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes required components
        /// </summary>
        private void InitializeComponents()
        {
            // Create spawner if not assigned
            if (spawner == null)
            {
                spawner = GetComponent<Spawner>();
                if (spawner == null)
                {
                    spawner = gameObject.AddComponent<Spawner>();
                    LogDebug("Created Spawner component");
                }
            }
            
            // Build direction lookup
            BuildDirectionLookup();
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        /// <summary>
        /// Builds lookup dictionary for spawn directions
        /// </summary>
        private void BuildDirectionLookup()
        {
            m_DirectionLookup = new Dictionary<SpawnDirection, DirectionSpawnPoint>();
            
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    m_DirectionLookup[spawnPoint.Direction] = spawnPoint;
                }
            }
            
            LogDebug($"Registered {m_DirectionLookup.Count} spawn points");
        }
        
        /// <summary>
        /// Subscribes to necessary events
        /// </summary>
        private void SubscribeToEvents()
        {
            WaveSystemEvents.OnEntityDied += OnEntityDied;
        }
        
        #endregion
        
        #region Public Methods - Level Control
        
        /// <summary>
        /// Starts a new level
        /// </summary>
        public void StartLevel(LevelConfig level)
        {
            if (level == null)
            {
                Debug.LogError("[WaveManager] Cannot start null level!");
                return;
            }
            
            if (IsLevelActive)
            {
                Debug.LogWarning("[WaveManager] A level is already active! Stop it first.");
                return;
            }
            
            if (!level.Validate())
            {
                Debug.LogError($"[WaveManager] Level '{level.LevelName}' validation failed!");
                return;
            }
            
            if (!ValidateSpawnPoints(level))
            {
                Debug.LogError($"[WaveManager] Missing spawn points for level directions!");
                return;
            }
            
            currentLevel = level;
            m_LevelStartTime = Time.time;
            m_TotalEntitiesKilled = 0;
            m_CurrentWaveIndex = -1;
            
            SetSystemState(WaveSystemState.PreparingLevel);
            
            LogDebug($"Starting level: {level.LevelName}");
            WaveSystemEvents.TriggerLevelStarted(level);
            
            // Start first wave
            StartCoroutine(LevelSequence());
        }
        
        /// <summary>
        /// Stops the current level
        /// </summary>
        public void StopLevel()
        {
            if (!IsLevelActive) return;
            
            // Stop wave coroutine
            if (m_WaveCoroutine != null)
            {
                StopCoroutine(m_WaveCoroutine);
                m_WaveCoroutine = null;
            }
            
            // Cleanup current wave
            if (m_CurrentWaveState != null)
            {
                m_CurrentWaveState.Cleanup();
                m_CurrentWaveState = null;
            }
            
            SetSystemState(WaveSystemState.Idle);
            LogDebug("Level stopped");
        }
        
        /// <summary>
        /// Pauses the current level
        /// </summary>
        public void PauseLevel()
        {
            if (!IsLevelActive) return;
            
            Time.timeScale = 0f;
            LogDebug("Level paused");
        }
        
        /// <summary>
        /// Resumes the current level
        /// </summary>
        public void ResumeLevel()
        {
            Time.timeScale = 1f;
            LogDebug("Level resumed");
        }
        
        #endregion
        
        #region Level Sequence
        
        /// <summary>
        /// Main coroutine that handles the level sequence
        /// </summary>
        private IEnumerator LevelSequence()
        {
            // Small delay before starting
            yield return new WaitForSeconds(1f);
            
            // Process each wave
            for (int i = 0; i < currentLevel.WaveCount; i++)
            {
                m_CurrentWaveIndex = i;
                var waveConfig = currentLevel.GetWave(i);
                
                if (waveConfig == null) continue;
                
                // Start wave
                //SetWaveCount(CurrentWaveIndex + 1);
                yield return StartWave(waveConfig);
                
                // Wait for wave completion
                yield return WaitForWaveCompletion();
                
                // Wait between waves (except after last wave)
                if (i < currentLevel.WaveCount - 1)
                {
                    SetSystemState(WaveSystemState.WaitingBetweenWaves);
                    LogDebug($"Waiting {currentLevel.IntervalBetweenWaves}s before next wave");
                    yield return new WaitForSeconds(currentLevel.IntervalBetweenWaves);
                }
            }
            
            // Level complete
            OnLevelComplete();
        }
        
        /// <summary>
        /// Starts a new wave
        /// </summary>
        private IEnumerator StartWave(WaveConfig waveConfig)
        {
            LogDebug($"Starting wave : '{waveConfig.WaveName}'");
            
            SetSystemState(WaveSystemState.WaveInProgress);
            
            // Create wave state
            m_CurrentWaveState = new WaveState(waveConfig);
            m_CurrentWaveState.Start();
            
            WaveSystemEvents.TriggerWaveStarted(m_CurrentWaveIndex, waveConfig);
            
            // Start spawning coroutine
            m_WaveCoroutine = StartCoroutine(WaveSpawnSequence());
            
            yield return null;
        }
        
        /// <summary>
        /// Handles the spawning sequence for a wave
        /// </summary>
        private IEnumerator WaveSpawnSequence()
        {
            while (m_CurrentWaveState != null && !m_CurrentWaveState.AllEntitiesSpawned)
            {
                // Get groups ready to spawn
                var readyGroups = m_CurrentWaveState.GetGroupsReadyToSpawn();
                
                foreach (var (group, count) in readyGroups)
                {
                    SpawnGroupBatch(group, count);
                }
                
                yield return null;
            }
            
            // Spawn complete
            if (m_CurrentWaveState != null)
            {
                WaveSystemEvents.TriggerWaveSpawnComplete(m_CurrentWaveIndex, m_CurrentWaveState.Config);
            }
        }
        
        /// <summary>
        /// Spawns a batch of entities for a group
        /// </summary>
        private void SpawnGroupBatch(SpawnGroupConfig group, int totalCount)
        {
            // Distribute entities across active directions
            var distribution = currentLevel.DistributeEntities(totalCount);
            var spawnedEntities = new List<Enemy>();
            
            foreach (var kvp in distribution)
            {
                var direction = kvp.Key;
                var count = kvp.Value;
                
                if (count <= 0) continue;
                
                if (m_DirectionLookup.TryGetValue(direction, out var spawnPoint))
                {
                    // Spawn entities at this direction
                    var entities = spawner.SpawnEntities(
                        group.EntityPrefab, 
                        count, 
                        spawnPoint.SpawnArea.bounds,
                        group.EntityPrefab.m_Type
                    );
                    
                    spawnedEntities.AddRange(entities);
                    
                    WaveSystemEvents.TriggerSpawnBatch(direction, count);
                    LogDebug($"Spawned {count} entities at {direction}");
                }
                else
                {
                    Debug.LogError($"[WaveManager] No spawn point found for direction: {direction}");
                }
            }
            
            // Record spawn in wave state
            if (m_CurrentWaveState != null && spawnedEntities.Count > 0)
            {
                m_CurrentWaveState.RecordSpawn(group, spawnedEntities);
                WaveSystemEvents.TriggerEntitiesSpawned(spawnedEntities.ToArray());
            }
        }
        
        /// <summary>
        /// Waits for the current wave to complete
        /// </summary>
        private IEnumerator WaitForWaveCompletion()
        {
            if (startNextWaveOnSpawnComplete)
            {
                // Wait for EITHER SpawnComplete OR Complete
                while (m_CurrentWaveState != null && 
                       m_CurrentWaveState.Status != WaveStatus.Complete &&
                       m_CurrentWaveState.Status != WaveStatus.SpawnComplete)
                {
                    yield return null;
                }
        
               
                if (m_CurrentWaveState != null)
                {
                    // Force complete if only spawn is done
                    if (m_CurrentWaveState.Status == WaveStatus.SpawnComplete)
                    {
                        // Log what triggered the progression
                        LogDebug($"Wave '{ m_CurrentWaveState.Config.WaveName}' progressing due to spawn completion");
                        // Force complete status for consistency
                        m_CurrentWaveState.Complete();
                    }
                    else
                    {
                        // Log what triggered the progression
                        LogDebug($"Wave '{m_CurrentWaveState.Config.WaveName}' completed - due to all entities dead");
                    }
                }
            }
            else
            {
                // Wait only for Complete (all dead)
                while (m_CurrentWaveState != null && m_CurrentWaveState.Status != WaveStatus.Complete)
                {
                    yield return null;
                }
        
                LogDebug($"Wave {m_CurrentWaveIndex + 1} completed - all entities defeated");
            }
    
            // Trigger completion event
            if (m_CurrentWaveState != null)
            {
                WaveSystemEvents.TriggerWaveCompleted(m_CurrentWaveIndex, m_CurrentWaveState.Config);
                m_CurrentWaveState = null;
            }
        }

        private void SetWaveCount(int count)
        {
            waveCountTxt.text = "Wave " + count + " /" + m_LevelConfigs.Length;
        }
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles entity death events
        /// </summary>
        private void OnEntityDied(Enemy entity)
        {
            m_TotalEntitiesKilled++;
            
            // Check if this completes the current wave
            if (m_CurrentWaveState != null && m_CurrentWaveState.AreAllEntitiesDead)
            {
                m_CurrentWaveState.Complete();
            }
        }

        /// <summary>
        /// Called when the level is completed successfully
        /// </summary>
        private void OnLevelComplete()
        {
            SetSystemState(WaveSystemState.LevelComplete);
            LogDebug($"Level '{currentLevel.LevelName}' completed!");
            WaveSystemEvents.TriggerLevelCompleted(currentLevel);


            if (m_CurrentLevelIndex < m_LevelConfigs.Length - 1)
            {
                m_CurrentLevelIndex++;
                SetWaveCount(m_CurrentLevelIndex + 1);
                currentLevel = m_LevelConfigs[m_CurrentLevelIndex];
                StartLevel(currentLevel);
            }
            else
            {
                LogDebug("All levels completed!");
                //GameManager.Instance.ShowVictoryScreen();
            }
        }
        
        /// <summary>
        /// Called when the level fails
        /// </summary>
        private void OnLevelFailed()
        {
            SetSystemState(WaveSystemState.LevelFailed);
            LogDebug($"Level '{currentLevel.LevelName}' failed!");
            WaveSystemEvents.TriggerLevelFailed(currentLevel);
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Sets the system state and triggers events
        /// </summary>
        private void SetSystemState(WaveSystemState newState)
        {
            if (m_SystemState != newState)
            {
                m_SystemState = newState;
                WaveSystemEvents.TriggerSystemStateChanged(newState);
            }
        }
        
        /// <summary>
        /// Updates and broadcasts statistics
        /// </summary>
        private void UpdateStatistics()
        {
            if (Time.time - m_LastStatisticsUpdate < statisticsUpdateInterval)
                return;
            
            m_LastStatisticsUpdate = Time.time;
            
            var stats = new WaveStatistics
            {
                CurrentWaveIndex = m_CurrentWaveIndex,
                TotalWaves = currentLevel != null ? currentLevel.WaveCount : 0,
                EntitiesSpawned = m_CurrentWaveState != null ? m_CurrentWaveState.TotalSpawned : 0,
                EntitiesAlive = m_CurrentWaveState != null ? m_CurrentWaveState.AliveCount : 0,
                EntitiesKilled = m_TotalEntitiesKilled,
                TimeElapsed = Time.time - m_LevelStartTime,
                SystemState = m_SystemState
            };
            
            WaveSystemEvents.TriggerStatisticsUpdated(stats);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates that all required spawn points exist
        /// </summary>
        private bool ValidateSpawnPoints(LevelConfig level)
        {
            var requiredDirections = level.SpawnDirections.GetIndividualDirections();
            
            foreach (var direction in requiredDirections)
            {
                if (!m_DirectionLookup.ContainsKey(direction))
                {
                    Debug.LogError($"[WaveManager] Missing spawn point for direction: {direction}");
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Logs debug messages if enabled
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[WaveManager] {message}");
            }
        }
        
        /// <summary>
        /// Gets current status for debugging
        /// </summary>
        public string GetDebugStatus()
        {
            if (m_CurrentWaveState != null)
            {
                return m_CurrentWaveState.GetDebugInfo();
            }
            
            return $"State: {m_SystemState}";
        }
        
#if UNITY_EDITOR
        private void OnGUI()
        {
            return;
            if (!showDebugUI || !IsLevelActive) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("Wave System Status");
            
            GUILayout.Label($"Level: {currentLevel?.LevelName ?? "None"}");
            GUILayout.Label($"State: {m_SystemState}");
            GUILayout.Label($"Wave: {m_CurrentWaveIndex + 1}/{currentLevel?.WaveCount ?? 0}");
            
            if (m_CurrentWaveState != null)
            {
                GUILayout.Label(m_CurrentWaveState.GetDebugInfo());
            }
            
            GUILayout.Label($"Total Killed: {m_TotalEntitiesKilled}");
            GUILayout.Label($"Time: {Time.time - m_LevelStartTime:F1}s");
            
            GUILayout.EndArea();
        }
#endif
        
        #endregion
    }
}