using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainPainterAStar
{
    [CreateAssetMenu(fileName = "AStarSettings", menuName = "Scriptable Objects/AStarSettings", order = 0)]
    public class PathfinderSettings : ScriptableObject
    {
        [SerializeField, Range(0, float.MaxValue), Tooltip("Movement speed multiplier (0 - float.MaxValue)")]
        private float grassMoveSpeed = 1, sandMoveSpeed = 1, stoneMoveSpeed = 0;

        public float GrassMoveSpeed => grassMoveSpeed;
        public float SandMoveSpeed => sandMoveSpeed;
        public float StoneMoveSpeed => stoneMoveSpeed;
    }
}
