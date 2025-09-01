using System;
using UnityEngine;
using WaveSystem.Core;
using WaveSystem.Data;

namespace WaveSystem.Events
{
    /// <summary>
    /// Centralized event system for wave-related events.
    /// Provides decoupled communication between wave system components.
    /// </summary>
    public static class WaveSystemEvents
    {
        #region Level Events
        
        /// <summary>
        /// Fired when a level starts
        /// </summary>
        public static event Action<LevelConfig> OnLevelStarted;
        
        /// <summary>
        /// Fired when a level is completed
        /// </summary>
        public static event Action<LevelConfig> OnLevelCompleted;
        
        /// <summary>
        /// Fired when a level fails
        /// </summary>
        public static event Action<LevelConfig> OnLevelFailed;
        
        #endregion
        
        #region Wave Events
        
        /// <summary>
        /// Fired when a wave starts
        /// </summary>
        public static event Action<int, WaveConfig> OnWaveStarted;
        
        /// <summary>
        /// Fired when all entities in a wave have been spawned
        /// </summary>
        public static event Action<int, WaveConfig> OnWaveSpawnComplete;
        
        /// <summary>
        /// Fired when a wave is completed (all entities dead)
        /// </summary>
        public static event Action<int, WaveConfig> OnWaveCompleted;
        
        /// <summary>
        /// Fired when wave progress changes
        /// </summary>
        public static event Action<int, int, int> OnWaveProgressChanged; // waveIndex, spawned, total
        
        #endregion
        
        #region Spawn Events
        
        /// <summary>
        /// Fired when an entity is spawned
        /// </summary>
        public static event Action<Enemy> OnEntitySpawned;
        
        /// <summary>
        /// Fired when multiple entities are spawned
        /// </summary>
        public static event Action<Enemy[]> OnEntitiesSpawned;
        
        /// <summary>
        /// Fired when an entity dies
        /// </summary>
        public static event Action<Enemy> OnEntityDied;
        
        /// <summary>
        /// Fired when spawn batch occurs
        /// </summary>
        public static event Action<SpawnDirection, int> OnSpawnBatch; // direction, count
        
        #endregion
        
        #region State Events
        
        /// <summary>
        /// Fired when the wave system state changes
        /// </summary>
        public static event Action<WaveSystemState> OnSystemStateChanged;
        
        /// <summary>
        /// Fired periodically with wave statistics
        /// </summary>
        public static event Action<WaveStatistics> OnStatisticsUpdated;
        
        #endregion
        
        #region Event Triggers - Level
        
        public static void TriggerLevelStarted(LevelConfig level)
        {
            OnLevelStarted?.Invoke(level);
        }
        
        public static void TriggerLevelCompleted(LevelConfig level)
        {
            OnLevelCompleted?.Invoke(level);
        }
        
        public static void TriggerLevelFailed(LevelConfig level)
        {
            OnLevelFailed?.Invoke(level);
        }
        
        #endregion
        
        #region Event Triggers - Wave
        
        public static void TriggerWaveStarted(int waveIndex, WaveConfig wave)
        {
            OnWaveStarted?.Invoke(waveIndex, wave);
        }
        
        public static void TriggerWaveSpawnComplete(int waveIndex, WaveConfig wave)
        {
            OnWaveSpawnComplete?.Invoke(waveIndex, wave);
        }
        
        public static void TriggerWaveCompleted(int waveIndex, WaveConfig wave)
        {
            OnWaveCompleted?.Invoke(waveIndex, wave);
        }
        
        public static void TriggerWaveProgressChanged(int waveIndex, int spawned, int total)
        {
            OnWaveProgressChanged?.Invoke(waveIndex, spawned, total);
        }
        
        #endregion
        
        #region Event Triggers - Spawn
        
        public static void TriggerEntitySpawned(Enemy entity)
        {
            OnEntitySpawned?.Invoke(entity);
        }
        
        public static void TriggerEntitiesSpawned(Enemy[] entities)
        {
            OnEntitiesSpawned?.Invoke(entities);
        }
        
        public static void TriggerEntityDied(Enemy entity)
        {
            OnEntityDied?.Invoke(entity);
        }
        
        public static void TriggerSpawnBatch(SpawnDirection direction, int count)
        {
            OnSpawnBatch?.Invoke(direction, count);
        }
        
        #endregion
        
        #region Event Triggers - State
        
        public static void TriggerSystemStateChanged(WaveSystemState state)
        {
            OnSystemStateChanged?.Invoke(state);
        }
        
        public static void TriggerStatisticsUpdated(WaveStatistics stats)
        {
            OnStatisticsUpdated?.Invoke(stats);
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clears all event subscriptions
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            // Level Events
            OnLevelStarted = null;
            OnLevelCompleted = null;
            OnLevelFailed = null;
            
            // Wave Events
            OnWaveStarted = null;
            OnWaveSpawnComplete = null;
            OnWaveCompleted = null;
            OnWaveProgressChanged = null;
            
            // Spawn Events
            OnEntitySpawned = null;
            OnEntitiesSpawned = null;
            OnEntityDied = null;
            OnSpawnBatch = null;
            
            // State Events
            OnSystemStateChanged = null;
            OnStatisticsUpdated = null;
        }
        
        #endregion
    }
    
    /// <summary>
    /// States of the wave system
    /// </summary>
    public enum WaveSystemState
    {
        Idle,
        PreparingLevel,
        WaitingForWaveStart,
        WaveInProgress,
        WaitingBetweenWaves,
        LevelComplete,
        LevelFailed
    }
    
    /// <summary>
    /// Statistics for the current wave/level
    /// </summary>
    [System.Serializable]
    public struct WaveStatistics
    {
        public int CurrentWaveIndex;
        public int TotalWaves;
        public int EntitiesSpawned;
        public int EntitiesAlive;
        public int EntitiesKilled;
        public float TimeElapsed;
        public WaveSystemState SystemState;
        
        public override string ToString()
        {
            return $"Wave {CurrentWaveIndex + 1}/{TotalWaves} - " +
                   $"Spawned: {EntitiesSpawned}, Alive: {EntitiesAlive}, Killed: {EntitiesKilled}";
        }
    }
}