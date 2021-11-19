using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        public bool walkingOver;

        public PathNode previous;
        public PathNode next;

        public void CalculateFCost()
        {
            f = (g + h);
        }

        // Debugging
        public void DrawGizmos(Vector3 a_position)
        {
            if (!isWalkable)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(a_position, 0.3f);
            }

            if (walkingOver)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(a_position, Vector3.one * 0.1f);
            }
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

        public bool platformer = false;

        private List<PathNode> openList;
        private List<PathNode> closedList;

        private Dictionary<Vector3, PathNode> tiledNodes;

        public PathNode CreateNode(Vector3 a_pos, bool a_walkable, PathNode a_previous = null)
        {
            PathNode node = new PathNode();
            node.x = (int)a_pos.x;
            node.y = (int)a_pos.y;

            node.g = int.MaxValue;
            node.CalculateFCost();
            node.previous = a_previous;

            node.isWalkable = a_walkable;

            tiledNodes[a_pos] = node;

            return node;
        }

        public void ResetTileNodes()
        {
            foreach (var t in tiledNodes)
            {
                var tile = t.Value;

                tile.g = int.MaxValue;
                tile.h = 0;
                tile.previous = null;
                tile.walkingOver = false;
                tile.CalculateFCost();
            }
        }

        public void LoadTiledNodes()
        {
            tiledNodes = new Dictionary<Vector3, PathNode>();

            foreach (var tile in map.loadedTiles)
            {
                CreateNode(tile.Key, false);
            }

            Vector2Int cellCount = map.GetGridCount();
            Vector3 cellSize = map.GetCellSize();

            for (int x = 0; x < cellCount.x; x++)
            {
                for (int y = 0; y < cellCount.y; y++)
                {
                    Vector3 pos = map.GetGridPosition(x, y);

                    if (!tiledNodes.ContainsKey(pos))
                        CreateNode(pos, true);
                }
            }
        }

        public PathNode GetNodeByTile(Vector3 a_position)
        {
            var tile = map.GetTileByClosestPosition(a_position);

            if (tile.prefabIndex == -1)
                return null;

            Vector3 position = tile.position;

            if (tiledNodes != null && tiledNodes.ContainsKey(position))
            {
                return tiledNodes[position];
            }

            return null;
        }

        public List<PathNode> FindMapPath(Vector3 a_start, Vector3 a_goal)
        {
            ResetTileNodes();

            PathNode startNode = GetNodeByTile(a_start);
            if (startNode == null)
                return null;

            PathNode endNode = GetNodeByTile(a_goal);
            if (endNode == null)
                return null;

            openList = new List<PathNode>() { startNode };
            closedList = new List<PathNode>();

            startNode.g = 0;
            startNode.h = CalculateDistanceCost(startNode, endNode);
            startNode.CalculateFCost();

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);

                // figure this out? weird.

                if (currentNode == endNode)
                    // Reached final node.
                    return CalculatePath(endNode);

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
                {
                    if (neighbourNode == null || closedList.Contains(neighbourNode) || 
                        !neighbourNode.isWalkable) continue; // Obstacle rule

                    int tentativeGCost = currentNode.g + CalculateDistanceCost(currentNode, neighbourNode);

                    if (tentativeGCost < neighbourNode.g || !openList.Contains(neighbourNode))
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
                currentNode.previous.walkingOver = true;
                path.Add(currentNode.previous);
                currentNode = currentNode.previous;
            }

            path.Reverse();

            return path;
        }

        private PathNode SetupNode(PathNode a_toSetup, PathNode a_current, PathNode a_end)
        {
            a_toSetup.previous = a_current;
            a_toSetup.g = a_current.g + CalculateDistanceCost(a_current, a_toSetup);
            a_toSetup.h = CalculateDistanceCost(a_toSetup, a_end);
            a_toSetup.CalculateFCost();

            if (!openList.Contains(a_toSetup))
                openList.Add(a_toSetup);

            return a_toSetup;
        }

        public List<PathNode> FindMapPlatformerPath(Vector3 a_start, Vector3 a_goal)
        {
            ResetTileNodes();

            PathNode startNode = GetNodeByTile(a_start);
            if (startNode == null)
                return null;

            PathNode endNode = GetNodeByTile(a_goal);
            if (endNode == null)
                return null;

            openList = new List<PathNode>() { startNode };
            closedList = new List<PathNode>();

            startNode.g = 0;
            startNode.h = CalculateDistanceCost(startNode, endNode);
            startNode.CalculateFCost();

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);

                // figure this out? weird.

                if (currentNode == endNode)
                    // Reached final node.
                    return CalculatePath(endNode);

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (PathNode neighbourNode in GetNeighbourPlatformerList(currentNode))
                {
                    if (neighbourNode == null || closedList.Contains(neighbourNode)) continue; // Obstacle rule

                    // Platform rules

                    // neighbourNode.isWalkable

                    int tentativeGCost = currentNode.g + CalculateDistanceCost(currentNode, neighbourNode);

                    if (tentativeGCost < neighbourNode.g || !openList.Contains(neighbourNode))
                        SetupNode(neighbourNode, currentNode, endNode);
                }
            }

            return null;
        }

        private PathNode FindLowestNode(PathNode a_start)
        {
            var node = GetNodeByTile(new Vector2(a_start.x, a_start.y - 1));
            return node != null && node.isWalkable ? FindLowestNode(node) : a_start;
        }

        private List<PathNode> GetNeighbourPlatformerList(PathNode a_currentNode)
        {
            List<PathNode> neighbourList = new List<PathNode>();

            float gridx = map.mapInfo.grid.gridSize.x / 2;
            float gridy = map.mapInfo.grid.gridSize.y / 2;

            PathNode down = null;
            PathNode up = null;

            // Down
            if (a_currentNode.y - 1 >= -map.mapInfo.grid.gridSize.y)
            {
                down = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y - 1));
                if (down.isWalkable)
                    neighbourList.Add(down);
            }

            // Up
            if (a_currentNode.y + 1 < map.mapInfo.grid.gridSize.y)
            {
                up = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y + 1));
                if (down.isWalkable)
                    neighbourList.Add(up);
            }

            if (a_currentNode.x - 1 >= -gridx)
            {
                var left = GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y));

                if (left != null)
                {
                    var leftDown = GetNodeByTile(new Vector2(left.x, left.y - 1));
                    var leftUp = GetNodeByTile(new Vector2(left.x, left.y + 1));

                    if (left.isWalkable && leftDown != null)
                    {
                        // Single left
                        if (!leftDown.isWalkable)
                            neighbourList.Add(left);
                        else // Jump down lowest node
                        {
                            var node = FindLowestNode(left);
                            node.walkingOver = true;
                            neighbourList.Add(node);
                        }
                    }
                    // Left up
                    else if (!left.isWalkable && left != null && leftUp.isWalkable)
                    {
                        neighbourList.Add(left);
                    }
                }

                // Left Up
                if (a_currentNode.y + 1 < gridy &&
                    up != null && up.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y + 1)));
            }

            if (a_currentNode.x + 1 >= 0)
            {
                // Right
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y)));

                // Right Down
                if (a_currentNode.y - 1 >= -gridy &&
                    down != null && down.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y - 1)));

                Debug.Log(a_currentNode.y + " " + gridy);

                // Right Up
                if (a_currentNode.y + 1 < gridy &&
                    up != null && up.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y + 1)));
            }

            return neighbourList;
        }

        private List<PathNode> GetNeighbourList(PathNode a_currentNode)
        {
            List<PathNode> neighbourList = new List<PathNode>();

            float gridx = map.mapInfo.grid.gridSize.x / 2;
            float gridy = map.mapInfo.grid.gridSize.y / 2;

            PathNode down = null;
            PathNode up = null;

            // Down
            if (a_currentNode.y - 1 >= -map.mapInfo.grid.gridSize.y)
            {
                down = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y - 1));
                neighbourList.Add(down);
            }

            // Up
            if (a_currentNode.y + 1 < map.mapInfo.grid.gridSize.y)
            {
                up = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y + 1));
                neighbourList.Add(up);
            }

            if (a_currentNode.x - 1 >= -gridx)
            {
                // Left
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y)));

                // Left Down
                if (a_currentNode.y - 1 >= -gridy && 
                    down != null && down.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y - 1)));

                // Left Up
                if (a_currentNode.y + 1 < gridy && 
                    up != null && up.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x - 1, a_currentNode.y + 1)));
            }

            if (a_currentNode.x + 1 >= 0)
            {
                // Right
                neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y)));

                // Right Down
                if (a_currentNode.y - 1 >= -gridy && 
                    down != null && down.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y - 1)));

                // Right Up
                if (a_currentNode.y + 1 < gridy && 
                    up != null && up.isWalkable)
                    neighbourList.Add(GetNodeByTile(new Vector2(a_currentNode.x + 1, a_currentNode.y + 1)));
            }

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

            //if (a_right.x > a_left.x)
            //    Debug.Log(xDistance + " " + yDistance + " " + remaining);

            return (int)(diagonalCost * Mathf.Min(xDistance, yDistance) + straightCost * remaining);
        }

        // Cursed remove this
        private void Start()
        {
            LoadTiledNodes();
        }
        //

        private void OnDrawGizmos()
        {
            if (tiledNodes != null)
                foreach (var tile in tiledNodes)
                {
                    tile.Value.DrawGizmos(map.GetTileByClosestPosition(new Vector3(tile.Value.x, tile.Value.y)).position);
                }
        }
    }
}
