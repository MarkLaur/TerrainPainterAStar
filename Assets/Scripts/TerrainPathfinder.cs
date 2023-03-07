using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TerrainPainterAStar
{
    public class TerrainPathfinder : MonoBehaviour
    {
        [SerializeField]
        private Transform startPoint;

        [SerializeField]
        private Transform endPoint;

        [SerializeField]
        private PathfinderSettings settings;

        private Terrain terrain;

        #region Event Listeners

        private void AStarCompleteListener(AStarResult result)
        {
            Debug.Log($"AStar found path: {result.PathFound}");
        }

        #endregion

        private void Awake()
        {
            if(!TryGetComponent(out terrain))
            {
                Debug.LogError("Cannot find terrain!", this);
            }
        }

        private void Start()
        {
            Debug.Log("Starting AStar");

            //Convert 3D position to 2D because we want them from a top down perspective
            Vector2 startPointXZ = GetXZ(startPoint);
            Vector2 endpointXZ = GetXZ(endPoint);

            //TODO: Get start and end points relative to move speed array 0,0 pos

            TerrainData td = terrain.terrainData;
            int height = td.alphamapHeight;
            int width = td.alphamapWidth;
            float[,] nodeMoveSpeeds = new float[height, width];

            for(int i = 0; i < width; i++)
            {
                for(int j = 0; i < height; i++)
                {
                    //TODO: read terrain texture map and calculate move speed from material blend value
                    nodeMoveSpeeds[i, j] = 1f;
                }
            }

            //Star astar
            AStar aStar = new AStar(startPointXZ, endpointXZ, nodeMoveSpeeds);
            aStar.OnAstarComplete += AStarCompleteListener;
            aStar.Start();

            Debug.Log("AStar end");
        }

        /// <summary>
        /// Gets X and Z coordinate from transform's position.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        private Vector2 GetXZ(Transform transform)
        {
            return new Vector2(transform.position.x, transform.position.z);
        }
    }
}