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
        private SimplePriorityQueue<AStarNode, float> startOpen = new(), endOpen = new();

        private AStarNode start;
        private AStarNode end;

        private AStarNode startCurrent;
        private AStarNode endCurrent;

        public SimplePriorityQueue<AStarNode, float> StartOpen => startOpen;
        public SimplePriorityQueue<AStarNode, float> EndOpen => endOpen;
        public AStarNode StartCurrent => startCurrent;
        public AStarNode EndCurrent => endCurrent;

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

            end = new AStarNode(NodeAncestor.EndPoint, endPoint, CalculateHCost(endPoint, startPoint), nodeMoveSpeeds[endPoint.x, endPoint.y]);
            end.GCost = 0;
            nodes.Add(end.Pos, end);
            endOpen.EnqueueWithoutDuplicates(end, end.FCost);

            start = new AStarNode(NodeAncestor.StartPoint, startPoint, CalculateHCost(startPoint, endPoint), nodeMoveSpeeds[startPoint.x, startPoint.y]);
            start.GCost = 0;
            nodes.Add(start.Pos, start);
            startOpen.EnqueueWithoutDuplicates(start, start.FCost);
        }

        #region Private Methods

        private void AStarRoutine()
        {
            AStarResult result;

            //Get first element from the queue
            if (!startOpen.TryDequeue(out startCurrent))
            {
                result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                OnAstarComplete(result);
                return;
            }

            //Get first element from the queue
            if (!endOpen.TryDequeue(out endCurrent))
            {
                result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                OnAstarComplete(result);
                return;
            }

            List<AStarNode> finalPath;

            //Main A* loop
            while (true)
            {
                //Search exit conditions
                if(startCurrent == end)
                {
                    finalPath = end.GetPath(true);
                    break;
                }
                else if(endCurrent == start)
                {
                    finalPath = endCurrent.GetPath(false);
                    break;
                }
                //Search can be stopped when the other frontier is found.
                else if (endOpen.Contains(startCurrent))
                {
                    AStarNode end = FindOtherFrontier(startCurrent);
                    AStarNode newEnd = end.RevertPath();
                    AStarNode fullPath = startCurrent.AppendPath(newEnd);
                    finalPath = fullPath.GetPath(true);
                    break;
                }
                else if (startOpen.Contains(endCurrent))
                {
                    AStarNode start = FindOtherFrontier(endCurrent);
                    AStarNode newEnd = endCurrent.RevertPath();
                    AStarNode fullPath = start.AppendPath(newEnd);
                    finalPath = fullPath.GetPath(true);
                    break;
                }

                //TODO: run searches on different threads
                ProcessNode(startCurrent, startOpen);
                ProcessNode(endCurrent, endOpen);

                //Get first element from the queue
                if (!startOpen.TryDequeue(out startCurrent))
                {
                    result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                    OnAstarComplete(result);
                    return;
                }

                //Get first element from the queue
                if (!endOpen.TryDequeue(out endCurrent))
                {
                    result = new AStarResult(AStarResultMSG.OpenQueueEmpty, null);
                    OnAstarComplete(result);
                    return;
                }
            }

            result = new AStarResult(AStarResultMSG.PathFound, finalPath);
            OnAstarComplete(result);
        }

        /// <summary>
        /// Goes through the current node's neighbors and updates their gcosts.
        /// </summary>
        private void ProcessNode(AStarNode node, SimplePriorityQueue<AStarNode, float> queue)
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
                    Vector2Int neighborPos = node.Pos + neighborOffset;
                    //Debug.Log($"{IsValidIndex(neighborPos)} {neighborPos}");
                    if (!IsValidIndex(neighborPos)) continue;

                    //Get neighbor
                    AStarNode neighbor = GetOrCreateNode(neighborPos, node);

                    //No need to check anything if node isn't traversable or if it has already been closed.
                    //It is impossible for there to be a shorter path to a closed node.
                    //Debug.Log($"{neighbor.Traversable} {neighbor.Closed}");
                    if (!neighbor.Traversable || neighbor.Closed) continue;

                    //Calculate the neighbor's gcost through current node
                    bool diagonal = i != 0 && j != 0;
                    float offsetLength = diagonal ? 1.4f : 1;
                    float neighborNewGCost = node.GCost + offsetLength / neighbor.MoveSpeed;

                    //Check if the current node is a better path to neighbor
                    if (neighborNewGCost < neighbor.GCost)
                    {
                        neighbor.GCost = neighborNewGCost;
                        neighbor.Parent = node;

                        //Enqueue will return false if node is already in the queue, update the priority in that case
                        if (!queue.EnqueueWithoutDuplicates(neighbor, neighbor.FCost))
                        {
                            queue.UpdatePriority(neighbor, neighbor.FCost);
                        }
                    }
                }
            }

            node.Closed = true;
        }

        /// <summary>
        /// Returns the best adjacent node from the other bidirectional search frontier.
        /// </summary>
        /// <param name="node"></param>
        private AStarNode FindOtherFrontier(AStarNode node)
        {
            AStarNode bestNeighbor = null;

            //Iterate through neighboring coordinates
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    //Skip the node itself
                    if (i == 0 && j == 0) continue;

                    //Calculate neighbor pos and check that it is within world bounds
                    Vector2Int neighborOffset = new Vector2Int(i, j);
                    Vector2Int neighborPos = node.Pos + neighborOffset;
                    //Debug.Log($"{IsValidIndex(neighborPos)} {neighborPos}");
                    if (!IsValidIndex(neighborPos)) continue;

                    //Get neighbor
                    AStarNode neighbor = GetNode(neighborPos);

                    //Ignore non traversable nodes and nodes that are from this node's frontier
                    if (!neighbor.Traversable || neighbor.Ancestor == node.Ancestor) continue;

                    //Update best neighbor
                    if(bestNeighbor == null || neighbor.GCost < bestNeighbor.GCost) bestNeighbor = neighbor;
                }
            }

            if (bestNeighbor == null) throw new Exception("Couldn't find a neighbor belonging to other frontier");

            return bestNeighbor;
        }

        /// <summary>
        /// Finds and existing node.
        /// </summary>
        /// <param name="nodePos"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private AStarNode GetNode(Vector2Int nodePos)
        {
            if (!nodes.TryGetValue(nodePos, out AStarNode node))
            {
                throw new Exception("Couldn't find node at that position.");
            }

            return node;
        }

        /// <summary>
        /// Gets node from nodes, or creates it if it doesn't exist.
        /// </summary>
        /// <param name="nodePos"></param>
        /// <returns></returns>
        private AStarNode GetOrCreateNode(Vector2Int nodePos, AStarNode parent)
        {
            if(!nodes.TryGetValue(nodePos, out AStarNode node))
            {
                //Figure out the goal node of this tree
                AStarNode goal;
                if(parent.Ancestor == NodeAncestor.StartPoint)
                {
                    goal = end;
                }
                else
                {
                    goal = start;
                }

                node = new AStarNode(parent.Ancestor, nodePos, CalculateHCost(nodePos, goal.Pos), nodeMoveSpeeds[nodePos.x, nodePos.y]);
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

        private float CalculateHCost(Vector2Int node, Vector2Int endNode)
        {
            int dx = Math.Abs(node.x - endNode.x);
            int dy = Math.Abs(node.y - endNode.y);
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