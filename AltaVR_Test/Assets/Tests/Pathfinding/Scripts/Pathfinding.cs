using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.Pathfinding
{
    public class PathNode
    {
        public int
            x,
            y;
        
        public int
            g, 
            h, 
            f;

        public bool isWalkable;

        public PathNode previous;
        public PathNode next;

        public void CalculateFCost()
        {
            f = (g + h);
        }
    }

    public class Pathfinding : MonoBehaviour
    {
        public MapCreation.Map map;

        public int diagonalCost = 10;
        public int straightCost = 14;

        public Vector2 navigatorSize;
        public Vector3 start;
        public Vector3 goal;

        private List<PathNode> openList;
        private List<PathNode> closedList;

        private Dictionary<MapCreation.Tile, PathNode> tiledNodes;

        public void LoadTiledNodes()
        {
            tiledNodes = new Dictionary<MapCreation.Tile, PathNode>();

            foreach (var tile in map.loadedTiles)
            {
                Vector2 pos = tile.Value.data.position;

                PathNode node = new PathNode();
                node.x = (int)pos.x;
                node.y = (int)pos.y;

                node.g = int.MaxValue;
                node.CalculateFCost();

                node.isWalkable = true;

                tiledNodes[tile.Value] = node;
            }
        }

        public PathNode GetNodeByTile(Vector3 a_position)
        {
            Vector3 position = map.GetTileByClosestPosition(a_position).position;

            if (map.loadedTiles != null && map.loadedTiles.ContainsKey(position) &&
                tiledNodes.ContainsKey(map.loadedTiles[position]))
            {
                return tiledNodes[map.loadedTiles[position]];
            }

            return null;
        }

        public List<PathNode> FindMapPath(Vector3 a_start, Vector3 a_goal)
        {
            PathNode startNode = GetNodeByTile(a_start);
            if (startNode == null)
                return null;

            PathNode endNode = GetNodeByTile(a_goal);

            openList = new List<PathNode>() { startNode };
            closedList = new List<PathNode>();

            startNode.g = 0;
            startNode.h = CalculateDistanceCost(startNode, endNode);
            startNode.CalculateFCost();

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);

                if (currentNode == endNode)
                    // Reached final node.
                    return CalculatePath(endNode);

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
                {
                    if (closedList.Contains(neighbourNode)) continue;

                    int tentativeGCost = currentNode.g + CalculateDistanceCost(currentNode, neighbourNode);

                    if (tentativeGCost < neighbourNode.g)
                    {
                        neighbourNode.previous = currentNode;
                        neighbourNode.g = tentativeGCost;
                        neighbourNode.h = CalculateDistanceCost(neighbourNode, endNode);
                        neighbourNode.CalculateFCost();

                        if (!openList.Contains(neighbourNode))
                            openList.Add(neighbourNode);
                    }
                }
            }

            return null;
        }

        private List<PathNode> CalculatePath(PathNode a_endNode)
        {
            List<PathNode> path = new List<PathNode>();
            path.Add(a_endNode);

            PathNode currentNode = a_endNode;
            while (currentNode.previous != null)
            {
                path.Add(currentNode.previous);
                currentNode = currentNode.previous;
            }

            path.Reverse();

            return path;
        }

        private List<PathNode> GetNeighbourList(PathNode a_currentNode)
        {
            List<PathNode> neighbourList = new List<PathNode>();

            if (a_currentNode.x - 1 >= 0)
            {
                // Left
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y)));

                // Left Down
                if (a_currentNode.y - 1 >= 0) 
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y - 1)));

                // Left Up
                if (a_currentNode.y + 1 >= map.mapInfo.grid.gridSize.y)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y + 1)));
            }
            if (a_currentNode.x - 1 >= map.mapInfo.grid.gridSize.x)
            {
                // Right
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y)));

                // Right Down
                if (a_currentNode.y - 1 >= 0)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y - 1)));

                // Right Up
                if (a_currentNode.y + 1 >= map.mapInfo.grid.gridSize.y)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y + 1)));
            }

            // Down
            if (a_currentNode.y - 1 >= 0)
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y - 1)));

            // Up
            if (a_currentNode.y + 1 < map.mapInfo.grid.gridSize.y)
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y + 1)));

            return neighbourList;
        }

        private PathNode GetLowestFCostNode(List<PathNode> a_pathNodeList)
        {
            PathNode lowestFNode = a_pathNodeList[0];

            for (int i = 1; i < a_pathNodeList.Count; i++)
            {
                if (a_pathNodeList[i].f < lowestFNode.f)
                    lowestFNode = a_pathNodeList[i];
            }

            return lowestFNode;
        }

        private int CalculateDistanceCost(PathNode a_left, PathNode a_right)
        {
            float xDistance = Mathf.Abs(a_left.x - a_right.x);
            float yDistance = Mathf.Abs(a_left.y - a_right.y);
            float remaining = Mathf.Abs(xDistance - yDistance);

            return (int)(diagonalCost * Mathf.Min(xDistance, yDistance) + straightCost * remaining);
        }

        // Cursed remove this
        private void Start()
        {
            StartCoroutine(LateLoad(0.5f));
        }

        private IEnumerator LateLoad(float a_time)
        {
            yield return new WaitForSeconds(a_time);

            LoadTiledNodes();
        }
//


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                var path = FindMapPath(map.mapInfo.tiles[0].position, mouseWorldPos);
                if (path != null)
                {
                    for (int i=0; i < path.Count -1; i++)
                    {
                        Debug.DrawLine(new Vector3(path[i].x, path[i].y) * 10f + Vector3.one * 5f, new Vector3(path[i + 1].x, path[i + 1].y) * 10f + Vector3.one * 5f);
                    }
                }
            }
        }
    }
}
