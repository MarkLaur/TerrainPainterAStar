using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TerrainPainterAStar
{
    public class AStar
    {
        /// <summary>
        /// Invoked when the pathfinding has completed.
        /// </summary>
        public event Action<AStarResult> OnAstarComplete;

        /// <summary>
        /// NodeMoveSpeeds is an array that contains the movement multipliers of each node.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="nodesMoveSpeeds"></param>
        public AStar(Vector2 startPoint, Vector2 endPoint, float[,] nodesMoveSpeeds)
        {

        }

        /// <summary>
        /// Starts the pathfinding algorithm.
        /// </summary>
        public void Start()
        {
            OnAstarComplete(new AStarResult(true));
        }
    }
}