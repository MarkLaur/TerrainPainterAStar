using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace TerrainPainterAStar
{
    public class AStarNode
    {
        private AStarNode parent;
        public AStarNode Parent { get => parent; set => parent = value; }
        public Vector2Int Pos { get; private set; }

        public bool Closed { get; set; }
        public bool Traversable { get; private set; }

        /// <summary>
        /// Estimated distance to end node.
        /// </summary>
        public float HCost { get; private set; }
        /// <summary>
        /// The graph distance to the node.
        /// </summary>
        public int GCost { get; set; } = int.MaxValue;
        public float FCost => HCost + GCost;

        public AStarNode(Vector2Int pos, float hCost, bool traversable)
        {
            Pos = pos;
            HCost = hCost;
            Traversable = traversable;
        }

        /// <summary>
        /// Returns the path from start to this node as a list.
        /// </summary>
        /// <returns></returns>
        public List<AStarNode> GetPath()
        {
            List<AStarNode> path = new();
            AStarNode current = this;

            //Traverse path and add nodes to list until end is found
            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            //Reverse path to make it start from the start
            path.Reverse();

            return path;
        }
    }
}
