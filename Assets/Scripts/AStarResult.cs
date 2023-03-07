using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainPainterAStar
{
    public class AStarResult
    {
        public bool PathFound { get; private set; }

        public AStarResult(bool pathFound)
        {
            PathFound = pathFound;
        }
    }
}