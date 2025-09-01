namespace WaveSystem.Core
{
    /// <summary>
    /// Represents the cardinal directions from which enemies can spawn
    /// </summary>
    [System.Flags]
    public enum SpawnDirection
    {
        None = 0,
        North = 1 << 0,  // 1
        West = 1 << 1,   // 2
        South = 1 << 2,  // 4
        East = 1 << 3    // 8
    }
    
    /// <summary>
    /// Extension methods for SpawnDirection enum
    /// </summary>
    public static class SpawnDirectionExtensions
    {
        /// <summary>
        /// Gets the number of active directions in the flag
        /// </summary>
        public static int GetActiveDirectionCount(this SpawnDirection directions)
        {
            int count = 0;
            if ((directions & SpawnDirection.North) != 0) count++;
            if ((directions & SpawnDirection.West) != 0) count++;
            if ((directions & SpawnDirection.South) != 0) count++;
            if ((directions & SpawnDirection.East) != 0) count++;
            return count;
        }
        
        /// <summary>
        /// Gets an array of individual directions from the flag
        /// </summary>
        public static SpawnDirection[] GetIndividualDirections(this SpawnDirection directions)
        {
            var list = new System.Collections.Generic.List<SpawnDirection>();
            
            if ((directions & SpawnDirection.North) != 0) list.Add(SpawnDirection.North);
            if ((directions & SpawnDirection.West) != 0) list.Add(SpawnDirection.West);
            if ((directions & SpawnDirection.South) != 0) list.Add(SpawnDirection.South);
            if ((directions & SpawnDirection.East) != 0) list.Add(SpawnDirection.East);
            
            return list.ToArray();
        }
    }
}