using UnityEngine;
using WaveSystem.Core;

namespace WaveSystem.Spawning
{
    /// <summary>
    /// Represents a spawn point for a specific direction.
    /// Attach this to a GameObject with a BoxCollider to define the spawn area.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DirectionSpawnPoint : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SpawnDirection direction = SpawnDirection.North;
        
        [Header("Visual Settings")]
        [SerializeField] private Color gizmoColor = Color.red;
        [SerializeField] private bool showGizmos = true;
        
        private BoxCollider m_SpawnArea;
        
        #region Properties
        
        /// <summary>
        /// The direction this spawn point represents
        /// </summary>
        public SpawnDirection Direction => direction;
        
        /// <summary>
        /// The box collider defining the spawn area
        /// </summary>
        public BoxCollider SpawnArea
        {
            get
            {
                if (m_SpawnArea == null)
                    m_SpawnArea = GetComponent<BoxCollider>();
                return m_SpawnArea;
            }
        }
        
        /// <summary>
        /// Gets the world-space bounds of the spawn area
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                if (SpawnArea == null) return new Bounds();
                
                return new Bounds(
                    transform.TransformPoint(SpawnArea.center),
                    Vector3.Scale(SpawnArea.size, transform.lossyScale)
                );
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            m_SpawnArea = GetComponent<BoxCollider>();
            
            // Ensure the collider is a trigger
            if (m_SpawnArea != null)
            {
                m_SpawnArea.isTrigger = true;
            }
        }
        
        private void OnValidate()
        {
            // Update GameObject name to reflect direction
            if (Application.isPlaying) return;
            
            gameObject.name = $"SpawnPoint_{direction}";
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Gets a random position within the spawn area
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            Bounds bounds = WorldBounds;
            
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        
        /// <summary>
        /// Validates if a position is within the spawn area
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            return WorldBounds.Contains(position);
        }
        
        #endregion
        
        #region Debug Visualization
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw spawn area bounds
            Gizmos.color = gizmoColor;
            
            if (SpawnArea != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(SpawnArea.center, SpawnArea.size);
                Gizmos.matrix = oldMatrix;
                
                // Draw direction indicator
                Vector3 center = transform.TransformPoint(SpawnArea.center);
                Vector3 directionVector = GetDirectionVector() * 2f;
                Gizmos.DrawLine(center, center + directionVector);
                
                // Draw arrow head
                Vector3 arrowEnd = center + directionVector;
                Vector3 arrowRight = Quaternion.Euler(0, 30, 0) * -directionVector * 0.3f;
                Vector3 arrowLeft = Quaternion.Euler(0, -30, 0) * -directionVector * 0.3f;
                Gizmos.DrawLine(arrowEnd, arrowEnd + arrowRight);
                Gizmos.DrawLine(arrowEnd, arrowEnd + arrowLeft);
            }
            
            // Draw label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Spawn: {direction}");
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            // Draw filled area when selected
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            
            if (SpawnArea != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(SpawnArea.center, SpawnArea.size);
                Gizmos.matrix = oldMatrix;
            }
        }
        
        /// <summary>
        /// Gets the direction vector based on spawn direction
        /// </summary>
        private Vector3 GetDirectionVector()
        {
            switch (direction)
            {
                case SpawnDirection.North:
                    return transform.forward;
                case SpawnDirection.South:
                    return transform.forward * -1;
                case SpawnDirection.East:
                    return transform.right;
                case SpawnDirection.West:
                    return transform.right * -1;
                default:
                    return Vector3.zero;
            }
        }
#endif
        
        #endregion
    }
}