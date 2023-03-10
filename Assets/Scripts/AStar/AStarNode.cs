using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainPainterAStar
{
    public enum NodeAncestor
    {
        StartPoint,
        EndPoint
    }

    public class AStarNode
    {
        private AStarNode parent;

        public NodeAncestor Ancestor { get; private set; }
        public AStarNode Parent { get => parent; 
            set 
            { 
                parent = value;
                Ancestor = value.Ancestor;
            }
        }
        public Vector2Int Pos { get; private set; }

        public bool Closed { get; set; }
        public float MoveSpeed { get; private set; }
        public bool Traversable => MoveSpeed != 0;

        /// <summary>
        /// Estimated distance to end node.
        /// </summary>
        public float HCost { get; private set; }
        /// <summary>
        /// The graph distance to the node.
        /// </summary>
        public float GCost { get; set; } = int.MaxValue;
        public float FCost => HCost + GCost;

        public AStarNode(NodeAncestor ancestor, Vector2Int pos, float hCost, float moveSpeed)
        {
            Ancestor = ancestor;
            Pos = pos;
            HCost = hCost;
            MoveSpeed = moveSpeed;
        }

        /// <summary>
        /// Returns the path from start to this node as a list.
        /// </summary>
        /// <returns></returns>
        public List<AStarNode> GetPath(bool startToEnd)
        {
            List<AStarNode> path = new();
            AStarNode current = this;

            //Traverse path and add nodes to list until end is found
            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            if (startToEnd)
            {
                //Reverse path to make it start from the start
                path.Reverse();
            }

            return path;
        }

        /// <summary>
        /// Reverts the node path, returns the new end node.
        /// </summary>
        /// <returns></returns>
        public AStarNode RevertPath()
        {
            float pathLength = this.GCost;
            AStarNode cur = this;
            AStarNode prev = null;
            while(cur != null)
            {
                cur.GCost = pathLength - cur.GCost;
                AStarNode next = cur.parent;
                cur.parent = prev;
                prev = cur;
                cur = next;
            }

            return prev;
        }

        /// <summary>
        /// Appends the path with given path. Returns the new path end.
        /// </summary>
        /// <returns></returns>
        public AStarNode AppendPath(AStarNode pathToAppend)
        {
            AStarNode cur = pathToAppend;
            do
            {
                cur.GCost += this.GCost;
                cur = cur.parent;
            }
            while (cur.parent != null);

            cur.parent = this;
            return pathToAppend;
        }

        public AStarNode GetFinalAncestor()
        {
            AStarNode cur = this;
            while (cur.parent != null) cur = cur.parent;
            return cur;
        }
    }
}
