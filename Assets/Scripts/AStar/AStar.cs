using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;

namespace TerrainPainterAStar
{
    /// <summary>
    /// Class that runs an A* algorithm.
    /// Simply pass the requred values to the constructor, set the OnAStarComplete callback and call Start().
    /// </summary>
    [System.Serializable]
    public class AStar
    {
        private Thread thread;

        private float[,] nodeMoveSpeeds;

        //TODO: Turn the lists into queues for better performance
        private Dictionary<Vector2Int, AStarNode> nodes = new();
        private List<AStarNode> open = new();

        private AStarNode start;
        private AStarNode end;

        private AStarNode current;

        public List<AStarNode> Open => open;
        public AStarNode Current => current;

        #region Events

        /// <summary>
        /// Invoked when the pathfinding has completed.
        /// </summary>
        public event Action<AStarResult> OnAstarComplete;

        #endregion

        /// <summary>
        /// NodeMoveSpeeds is an array that contains the movement multipliers of each node.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="nodesMoveSpeeds"></param>
        public AStar(Vector2Int startPoint, Vector2Int endPoint, float[,] nodesMoveSpeeds)
        {
            nodeMoveSpeeds = nodesMoveSpeeds;

            end = new AStarNode(endPoint, 0, IsTraversable(endPoint));
            nodes.Add(end.Pos, end);

            start = new AStarNode(startPoint, CalculateHCost(startPoint), IsTraversable(startPoint));
            nodes.Add(start.Pos, start);
            open.Add(start);
        }

        #region Private Methods

        private void AStarRoutine()
        {
            AStarResult result;

            if(!TryGetMinOpen(out current))
            {
                result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                OnAstarComplete(result);
                return;
            }

            while (true)
            {
                if (current == end)
                {
                    result = new AStarResult(AStarResultMSG.PathFound, end.GetPath());
                    break;
                }

                Debug.Log($"Processing node {current.Pos}");
                ProcessCurrentNode();
                Close(current);

                if (!TryGetMinOpen(out current))
                {
                    result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                    OnAstarComplete(result);
                    return;
                }
            }

            OnAstarComplete(result);
        }

        /// <summary>
        /// Goes through the current node's neighbors and updates their gcosts.
        /// </summary>
        private void ProcessCurrentNode()
        {
            //Iterate through neighboring coordinates
            for(int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    //Skip the node itself
                    if (i == 0 && j == 0) continue;

                    Vector2Int neighborOffset = new Vector2Int(i, j);
                    AStarNode neighbor = GetOrCreateNode(current.Pos + neighborOffset);

                    //No need to check anything if node isn't traversable or if it has already been closed.
                    //It is impossible for there to be a shorter path to a closed node.
                    if (!neighbor.Traversable || neighbor.Closed) continue;

                    //Calculate the neighbor's gcost through this node
                    bool diagonal = i == 0 || j == 0;
                    int offsetLength = diagonal ? 14 : 10;
                    int neighborNewGCost = current.GCost + offsetLength;

                    //Check if the current node is a better path to neighbor
                    if (neighborNewGCost < neighbor.GCost)
                    {
                        neighbor.GCost = neighborNewGCost;
                        if (!open.Contains(neighbor)) open.Add(neighbor);
                        //TODO: update neighbor queue value if it is already in queue.
                    }
                }
            }
        }

        private void Close(AStarNode node)
        {
            node.Closed = true;
        }

        /// <summary>
        /// Gets node from nodes, or creates it if it doesn't exist.
        /// </summary>
        /// <param name="nodePos"></param>
        /// <returns></returns>
        private AStarNode GetOrCreateNode(Vector2Int nodePos)
        {
            if(!nodes.TryGetValue(nodePos, out AStarNode node))
            {
                node = new AStarNode(nodePos, CalculateHCost(nodePos), IsTraversable(nodePos));
                nodes.Add(node.Pos, node);
            }

            return node;
        }

        /// <summary>
        /// Checks if position is traversable.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool IsTraversable(Vector2Int pos)
        {
            if(pos.x < 0 || pos.y < 0 || pos.x > nodeMoveSpeeds.GetLength(0) - 1 || pos.y > nodeMoveSpeeds.GetLength(1) - 1)
            {
                Debug.LogError($"Node pos exceeds the world bounds {pos} {nodeMoveSpeeds.GetLength(0) - 1}" + $" {nodeMoveSpeeds.GetLength(1) - 1}");
                return false;
            }
            
            return nodeMoveSpeeds[pos.x, pos.y] != 0f;
        }

        private float CalculateHCost(Vector2Int node)
        {
            int dx = Math.Abs(node.x - end.Pos.x);
            int dy = Math.Abs(node.y - end.Pos.y);
            return 10 * (dx + dy) + (14 - 2 * 10) * MathF.Min(dx, dy);
        }

        private bool TryGetMinOpen(out AStarNode minNode)
        {
            if (open.Count == 0)
            {
                minNode = null;
                return false;
            }

            minNode = open[0];

            foreach(AStarNode node in open)
            {
                if (node.FCost < minNode.FCost) minNode = node;
            }

            open.Remove(minNode);
            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the pathfinding algorithm.
        /// </summary>
        public void Start()
        {
            if (!start.Traversable)
            {
                OnAstarComplete(new AStarResult(AStarResultMSG.StartNotTraversable, null));
                return;
            }
            else if (!end.Traversable)
            {
                OnAstarComplete(new AStarResult(AStarResultMSG.EndNotTraversable, null));
                return;
            }

            if(thread != null && thread.ThreadState != ThreadState.Unstarted)
            {
                Debug.LogError("AStar has already been started. Make sure Start() is only called once.");
                return;
            }

            ThreadStart threadDelegate = new ThreadStart(AStarRoutine);
            thread = new Thread(threadDelegate);
            thread.Start();
        }

        public void Kill()
        {
            if(thread != null)
            {
                thread.Abort();
            }
        }

        #endregion
    }
}