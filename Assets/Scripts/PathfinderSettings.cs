using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainPainterAStar
{
    [CreateAssetMenu(fileName = "AStarSettings", menuName = "Scriptable Objects/AStarSettings", order = 0)]
    public class PathfinderSettings : ScriptableObject
    {
        //Max has to be 1 so that A* heuristic remains admissible (smaller than actual distance)
        [SerializeField, Range(0, 1), Tooltip("Movement speed multipliers (0 - 1)")]
        private float[] layerMoveSpeeds = {0, 1f, 0.5f};

        public float[] LayerMoveSpeeds => layerMoveSpeeds;
    }
}
