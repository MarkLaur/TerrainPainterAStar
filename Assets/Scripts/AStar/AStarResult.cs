using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainPainterAStar
{
    public enum AStarResultMSG
    {
        PathFound,
        /// <summary>
        /// Couldn't find path because open queue ran empty.
        /// </summary>
        OpenQueueEmpty,
        StartNotTraversable,
        EndNotTraversable
    }

    [System.Serializable]
    public class AStarResult
    {
        public bool PathFound => ResultMSG == AStarResultMSG.PathFound;
        public AStarResultMSG ResultMSG { get; private set; }
        public List<AStarNode> Path { get; private set; }

        public AStarResult(AStarResultMSG result, List<AStarNode> path)
        {
            ResultMSG= result;
            Path = path;
        }

        public override string ToString()
        {
            int pathLength = Path != null ? Path.Count : 0;
            return $"PathFound: {PathFound} | ResultMSG: {ResultMSG} | PathLength: {pathLength}";
        }
    }
}