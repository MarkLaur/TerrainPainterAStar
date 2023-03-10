using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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
        private SimplePriorityQueue<AStarNode, float> open = new();

        private AStarNode start;
        private AStarNode end;

        private AStarNode current;

        public SimplePriorityQueue<AStarNode, float> Open => open;
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

            end = new AStarNode(endPoint, 0, nodeMoveSpeeds[endPoint.x, endPoint.y]);
            nodes.Add(end.Pos, end);

            start = new AStarNode(startPoint, CalculateHCost(startPoint), nodeMoveSpeeds[startPoint.x, startPoint.y]);
            start.GCost = 0;
            nodes.Add(start.Pos, start);
            open.EnqueueWithoutDuplicates(start, start.FCost);
        }

        #region Private Methods

        private void AStarRoutine()
        {
            AStarResult result;

            //Get first element from the queue
            if (!open.TryDequeue(out current))
            {
                result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                OnAstarComplete(result);
                return;
            }

            //Main A* loop
            while (current != end)
            {
                ProcessCurrentNode();

                //Get first element from the queue
                if (!open.TryDequeue(out current))
                {
                    result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                    OnAstarComplete(result);
                    return;
                }
            }

            result = new AStarResult(AStarResultMSG.PathFound, end.GetPath());
            OnAstarComplete(result);
        }

        /// <summary>
        /// Goes through the current node's neighbors and updates their gcosts.
        /// </summary>
        private void ProcessCurrentNode()
        {
            //Iterate through neighboring coordinates
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    //Skip the node itself
                    if (i == 0 && j == 0) continue;

                    //Calculate neighbor pos and check that it is within world bounds
                    Vector2Int neighborOffset = new Vector2Int(i, j);
                    Vector2Int neighborPos = current.Pos + neighborOffset;
                    //Debug.Log($"{IsValidIndex(neighborPos)} {neighborPos}");
                    if (!IsValidIndex(neighborPos)) continue;

                    //Get neighbor
                    AStarNode neighbor = GetOrCreateNode(neighborPos);

                    //No need to check anything if node isn't traversable or if it has already been closed.
                    //It is impossible for there to be a shorter path to a closed node.
                    //Debug.Log($"{neighbor.Traversable} {neighbor.Closed}");
                    if (!neighbor.Traversable || neighbor.Closed) continue;

                    //Calculate the neighbor's gcost through current node
                    bool diagonal = i != 0 && j != 0;
                    float offsetLength = diagonal ? 1.4f : 1;
                    float neighborNewGCost = current.GCost + offsetLength / neighbor.MoveSpeed;

                    //Check if the current node is a better path to neighbor
                    if (neighborNewGCost < neighbor.GCost)
                    {
                        neighbor.GCost = neighborNewGCost;
                        neighbor.Parent = current;

                        //Enqueue will return false if node is already in the queue, update the priority in that case
                        if (!open.EnqueueWithoutDuplicates(neighbor, neighbor.FCost))
                        {
                            open.UpdatePriority(neighbor, neighbor.FCost);
                        }
                    }
                }
            }

            current.Closed = true;
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
                node = new AStarNode(nodePos, CalculateHCost(nodePos), nodeMoveSpeeds[nodePos.x, nodePos.y]);
                nodes.Add(node.Pos, node);
            }

            return node;
        }

        private bool IsValidIndex(Vector2Int pos)
        {
            return pos.x >= 0
                && pos.y >= 0
                && pos.x < nodeMoveSpeeds.GetLength(0)
                && pos.y < nodeMoveSpeeds.GetLength(1);
        }

        private float CalculateHCost(Vector2Int node)
        {
            int dx = Math.Abs(node.x - end.Pos.x);
            int dy = Math.Abs(node.y - end.Pos.y);
            float dist = 1 * (dx + dy) + (1.4f - 2 * 1) * Math.Min(dx, dy);
            return dist;
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