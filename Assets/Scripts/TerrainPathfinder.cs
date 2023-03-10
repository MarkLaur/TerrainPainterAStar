using System.Collections;
using System.Collections.Generic;
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

        [SerializeField]
        private Terrain terrain;

        private float pixelsPerUnitX, pixelsPerUnitY;

        private AStar aStar = default;
        private AStarResult result;

        #region Event Listeners

        private void AStarCompleteListener(AStarResult result)
        {
            Debug.Log($"AStar found path: {result}");
            this.result = result;
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

            //Get base map pixels per unit
            pixelsPerUnitX = terrain.terrainData.baseMapResolution / terrain.terrainData.size.x;
            pixelsPerUnitY = terrain.terrainData.baseMapResolution / terrain.terrainData.size.z;

            //Convert 3D position to 2D because we want them from a top down perspective
            Vector2 startPointXZ = GetXZ(startPoint);
            Vector2 endpointXZ = GetXZ(endPoint);

            //Transform world points to texture array space
            Vector2Int start = TransformToTerrainMap(startPointXZ);
            Vector2Int end = TransformToTerrainMap(endpointXZ);

            TerrainData td = terrain.terrainData;
            int basemapRes = td.baseMapResolution;
            float[,] nodeMoveSpeeds = new float[basemapRes, basemapRes];
            Color[] pixels = td.alphamapTextures[0].GetPixels();

            //Read terrain texture map and set move speed array values
            for (int i = 0; i < basemapRes; i++)
            {
                for(int j = 0; j < basemapRes; j++)
                {
                    //TODO: implement support for arbitrary layer count

                    //Add weighted move speeds
                    float weightedMoveSpeed =
                        pixels[i + basemapRes * j].r * settings.LayerMoveSpeeds[0]
                        + pixels[i + basemapRes * j].g * settings.LayerMoveSpeeds[1]
                        + pixels[i + basemapRes * j].b * settings.LayerMoveSpeeds[2];

                    //Normalize the move speed
                    float sum =
                        pixels[i + basemapRes * j].r
                        + pixels[i + basemapRes * j].g
                        + pixels[i + basemapRes * j].b;
                    weightedMoveSpeed /= sum;

                    nodeMoveSpeeds[i, j] = weightedMoveSpeed;
                }
            }

            //Start astar
            result = null;
            aStar = new AStar(start, end, nodeMoveSpeeds);
            aStar.OnAstarComplete += AStarCompleteListener;
            aStar.Start();

            Debug.Log("AStar started");
        }

        private void OnValidate()
        {
            if(TryGetComponent(out Terrain cache))
            {
                terrain = cache;
            }

            //Debug.Log(TransformToTerrainMap(GetXZ(startPoint)));
            //Debug.Log(TransformToTerrainMap(GetXZ(endPoint)));
        }

        private void OnDrawGizmos()
        {
            if (aStar == null || aStar.StartOpen == null) return;

            Gizmos.color = Color.gray;

            //Draw open queue nodes
            foreach (AStarNode node in aStar.StartOpen)
            {
                Gizmos.DrawSphere(TransformToWorld(node.Pos), 2);
            }

            //Draw open queue nodes
            foreach (AStarNode node in aStar.EndOpen)
            {
                Gizmos.DrawSphere(TransformToWorld(node.Pos), 2);
            }

            if (result == null)
            {
                //Draw current node path
                if (aStar.StartCurrent != null)
                {
                    Gizmos.color = Color.green;
                    AStarNode cur = aStar.StartCurrent;
                    while (cur != null)
                    {
                        Gizmos.DrawSphere(TransformToWorld(cur.Pos), 2);
                        cur = cur.Parent;
                    }
                }

                //Draw current end node path
                if (aStar.EndCurrent != null)
                {
                    Gizmos.color = Color.green;
                    AStarNode cur = aStar.EndCurrent;
                    while (cur != null)
                    {
                        Gizmos.DrawSphere(TransformToWorld(cur.Pos), 2);
                        cur = cur.Parent;
                    }
                }
            }
            else if(result.Path != null)
            {
                //Draw result
                Gizmos.color = Color.green;
                foreach (AStarNode node in result.Path)
                {
                    Gizmos.DrawSphere(TransformToWorld(node.Pos), 2);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (aStar != null) aStar.Kill();
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

        /// <summary>
        /// Transforms a point from splat map space to world space
        /// </summary>
        /// <returns></returns>
        private Vector3 TransformToWorld(Vector2Int point)
        {
            //Position = terrain pos + (point pos * point pos scale factor)
            return terrain.transform.position + new Vector3(point.x / pixelsPerUnitX, 0, point.y / pixelsPerUnitY);
        }

        /// <summary>
        /// Returns the closest terrain splat map point from a world space coordinate.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Vector2Int TransformToTerrainMap(Vector2 point)
        {
            //Transform the point to be relative to terrain corner
            point -= GetXZ(terrain.transform);

            //Round the point to the closest splat map coordinate.
            return new Vector2Int((int)(point.x * pixelsPerUnitX), (int)(point.y * pixelsPerUnitY));
        }
    }
}