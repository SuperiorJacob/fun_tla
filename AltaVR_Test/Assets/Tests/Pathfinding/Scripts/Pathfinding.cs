using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public bool reachableOutside;

        public PathNode previous;

        public void CalculateFCost()
        {
            f = (g + h);
        }

#if UNITY_EDITOR
        public PathNode next;

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

            if (reachableOutside)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(a_position, Vector3.one * 0.1f);
            }
        }
#endif
    }

    public class Pathfinding : MonoBehaviour
    {
        public MapCreation.Map map;
        public PlayerFinder player;

        public int diagonalCost = 10;
        public int straightCost = 14;

        public Vector2 navigatorSize;
        
        [HideInInspector] public Vector3 start;
        [HideInInspector] public Vector3 goal;

        public bool platformer = false;

#if UNITY_EDITOR
        public bool renderPaths;
#endif

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

        public void ResetTileNode(PathNode a_node)
        {
            a_node.g = int.MaxValue;
            a_node.h = 0;
            a_node.previous = null;
            a_node.walkingOver = false;
            a_node.CalculateFCost();
        }

        public void ResetTileNodes()
        {
            System.Action toChange = default;

            foreach (var t in tiledNodes)
            {
                var tile = t.Value;
                ResetTileNode(tile);

                // Create a tile that can only be accessed at the top for jumping.
                if (platformer && !tile.reachableOutside)
                {
                    // Invisible one up
                    Vector3 up = new Vector3(tile.x, tile.y + 1);
                    if (!tiledNodes.ContainsKey(up))
                    {
                        toChange += () => {
                            var node = CreateNode(up, true);
                            node.reachableOutside = true;

                            ResetTileNode(node);
                        };
                    }

                    // Invisible two up for jumping.
                    Vector3 up2 = new Vector3(up.x, up.y + 1);
                    if (!tiledNodes.ContainsKey(up2))
                    {
                        toChange += () => {
                            var node = CreateNode(up2, true);
                            node.reachableOutside = true;

                            ResetTileNode(node);
                        };
                    }
                }
            }

            toChange?.Invoke();
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

        public MapCreation.TileData GetNodeByClosestPosition(Vector3 a_position)
        {
            var tile = map.GetTileByClosestPosition(a_position);
            var node = tiledNodes.ContainsKey(a_position) ? tiledNodes[a_position] : null;

            if (node != null && node.reachableOutside)
                tile.position = new Vector3(node.x, node.y);

            return tile;
        }

        public PathNode GetNodeByTile(Vector3 a_position)
        {
            var tile = GetNodeByClosestPosition(a_position);

            var node = tiledNodes.ContainsKey(tile.position) ? tiledNodes[tile.position] : null;

            if (tile.prefabIndex == -1 && (node == null || !node.reachableOutside))
                return null;

            return node;
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

            PathNode belowEndNode = GetNodeByTile(new Vector3(endNode.x, endNode.y - 1));
            if (platformer && (belowEndNode == null || belowEndNode.isWalkable)) // Check if the player can ACTUALLY stand above this node.
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

                foreach (PathNode neighbourNode in (platformer ? GetNeighbourPlatformerList(currentNode) : GetNeighbourList(currentNode)))
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
                if (!renderPaths || renderPaths && Application.isPlaying)
                    currentNode.walkingOver = true;

                path.Add(currentNode.previous);
                currentNode = currentNode.previous;
            }

            path.Reverse();

            return path;
        }

        private PathNode FindLowestNode(PathNode a_start)
        {
            var node = GetNodeByTile(new Vector2(a_start.x, a_start.y - 1));
            return node != null && node.isWalkable ? FindLowestNode(node) : a_start;
        }

        private PathNode FindHighestNode(PathNode a_start)
        {
            var node = GetNodeByTile(new Vector2(a_start.x, a_start.y + 1));

            if (node != null && !node.isWalkable)
            {
                var nodeRight = GetNodeByTile(new Vector2(a_start.x + 1, a_start.y));
                var nodeRightBelow = GetNodeByTile(new Vector2(a_start.x + 1, a_start.y - 1));

                if (nodeRight != null && nodeRight.isWalkable && nodeRightBelow != null && !nodeRightBelow.isWalkable)
                    return nodeRight;

                var nodeLeft = GetNodeByTile(new Vector2(a_start.x - 1, a_start.y));
                var nodeLeftBelow = GetNodeByTile(new Vector2(a_start.x - 1, a_start.y - 1));

                if (nodeLeft != null && nodeLeft.isWalkable && nodeLeftBelow != null && !nodeLeftBelow.isWalkable)
                    return nodeLeft;
            }

            return node != null && node.isWalkable ? FindHighestNode(node) : a_start;
        }

        private void GetDirectionedRules(List<PathNode> a_neighbourList, PathNode a_currentNode, PathNode a_up, PathNode a_down, float a_gridY, int a_dir)
        {
            var dirNode = GetNodeByTile(new Vector2(a_currentNode.x + a_dir, a_currentNode.y));

            if (dirNode != null)
            {
                var dirDown = GetNodeByTile(new Vector2(dirNode.x, dirNode.y - 1));
                var dirUp = GetNodeByTile(new Vector2(dirNode.x, dirNode.y + 1));

                // Order of rules:

                // First rule jumping 1

                var dirTwoDown = GetNodeByTile(new Vector2(dirNode.x + a_dir, dirNode.y - 1));

                if (a_up != null && a_up.isWalkable && // Can we go up? (jump)
                a_down != null && !a_down.isWalkable && // Is this actually a platform?
                dirDown != null && dirDown.isWalkable && // Can we continue jumping?
                dirUp != null && dirUp.isWalkable && 
                dirTwoDown != null && !dirTwoDown.isWalkable)  // We can land?
                    a_neighbourList.Add(dirUp);

                // First rule is walking across or falling directly down.
                if (dirNode.isWalkable && dirDown != null)
                {
                    // Single Dir
                    if (!dirDown.isWalkable)
                        a_neighbourList.Add(dirNode);
                    else // 1x Directional (jump down any distance).
                    {
                        // Directly down
                        var lowest = FindLowestNode(dirNode);
                        a_neighbourList.Add(lowest);

                        // 2x Directional (jump down any distance).
                        var lowestDir = GetNodeByTile(new Vector2(dirNode.x + a_dir, dirNode.y));
                        if (lowestDir != null)
                        {
                            lowestDir = FindLowestNode(lowestDir);

                            if (lowestDir != null && !lowestDir.isWalkable)
                                a_neighbourList.Add(lowestDir);
                        }
                    }
                }
                // Third rule is jumping up one.
                else if (!dirNode.isWalkable)
                {
                    // Jump up 1
                    if (a_up != null && a_up.isWalkable &&
                        dirUp != null && dirUp.isWalkable)
                    {
                        a_neighbourList.Add(dirUp);
                    }
                }

                // Fourth rule is jumping up two.
                if (a_up != null && a_up.isWalkable && dirUp != null && !dirUp.isWalkable)
                {
                    // Check if there are two spaces free above you.
                    var upTwo = GetNodeByTile(new Vector2(a_up.x, a_up.y + 1));

                    if (upTwo != null && upTwo.isWalkable)
                    {
                        // Jump up 2
                        var dirUpTwo = GetNodeByTile(new Vector2(dirUp.x, dirUp.y + 1));

                        if (dirUpTwo != null && dirUpTwo.isWalkable)
                            a_neighbourList.Add(dirUpTwo);
                    }
                }
            }
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
                down = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y - 1));

            // Up
            if (a_currentNode.y + 1 < map.mapInfo.grid.gridSize.y)
                up = GetNodeByTile(new Vector2(a_currentNode.x, a_currentNode.y + 1));
            
            // Left
            if (a_currentNode.x - 1 >= -gridx)
                GetDirectionedRules(neighbourList, a_currentNode, up, down, gridy, -1);

            // Right
            if (a_currentNode.x + 1 >= 0)
                GetDirectionedRules(neighbourList, a_currentNode, up, down, gridy, 1);

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

#if UNITY_EDITOR
        // All of this code below is for rendering scene paths.

        private List<(Vector3 start, Vector3 end)> _renderPathTest;

        private void AddRenderPathPosition(List<PathNode> a_pathRoute)
        {
            for (int i = 0; i < a_pathRoute.Count - 2; i++)
            {
                var pR = a_pathRoute[i];

                pR.next = a_pathRoute[i + 1];

                var pos1 = (new Vector3(pR.x, pR.y), new Vector3(pR.next.x, pR.next.y));

                if (!_renderPathTest.Contains(pos1))
                    _renderPathTest.Add(pos1);
            }
        }

        private void AddRenderPathDirectionDown(Vector3 a_position, int a_left)
        {
            var nodeDir = GetNodeByTile(new Vector3(a_position.x - a_left, a_position.y));
            if (nodeDir != null && nodeDir.isWalkable)
            {
                var lowestNodeDir = FindLowestNode(nodeDir);

                if (lowestNodeDir != null && lowestNodeDir.isWalkable && lowestNodeDir.y < nodeDir.y)
                {
                    (Vector3 start, Vector3 end) pos = (a_position, new Vector3(lowestNodeDir.x, lowestNodeDir.y));

                    if (!_renderPathTest.Contains(pos))
                    {
                        var fp = FindMapPath(pos.start, pos.end);
                        if (fp != null && fp.Count > 0)
                            _renderPathTest.Add(pos);
                    }
                }
            }

            // Link broken jumps. Don't ask lmfao.

            var nodeDirJump = GetNodeByTile(new Vector3(a_position.x - a_left, a_position.y - 1));
            var nodeDir2 = GetNodeByTile(new Vector3(a_position.x - (a_left * 2), a_position.y));
            var nodeDirDown = GetNodeByTile(new Vector3(a_position.x - (a_left * 2), a_position.y - 1));
            var nodeDirUp = GetNodeByTile(new Vector3(a_position.x - a_left, a_position.y + 1));

            if (nodeDirJump != null && nodeDirJump.isWalkable &&
                nodeDir2 != null && nodeDir2.isWalkable &&
                nodeDirDown != null && !nodeDirDown.isWalkable &&
                nodeDirUp != null && nodeDirUp.isWalkable)
            {
                (Vector3 start, Vector3 end) pos = (a_position, new Vector3(nodeDirUp.x, nodeDirUp.y));

                if (!_renderPathTest.Contains(pos))
                {
                    _renderPathTest.Add(pos);
                }
            }
        }

        public void GenerateRenderPath()
        {
            if (player == null)
                return;

            tiledNodes = new Dictionary<Vector3, PathNode>();

            map.LoadSavedTiles();
            LoadTiledNodes();
            ResetTileNodes();

            _renderPathTest = new List<(Vector3 start, Vector3 end)>();

            // This is ... pretty cursed, I couldn't think of how to do this properly.

            Vector3 startPos = player.GetPos();
            List<Vector3> posList = new List<Vector3>();

            foreach (var tile in tiledNodes)
            {
                if (platformer && !tile.Value.isWalkable)
                {
                    Vector3 p = GetNodeByClosestPosition(new Vector3(tile.Value.x, tile.Value.y + 1)).position;
                    if (!posList.Contains(p))
                        posList.Add(p);
                }
                else if (!platformer)
                {
                    Vector3 p = GetNodeByClosestPosition(new Vector3(tile.Value.x, tile.Value.y)).position;
                    if (!posList.Contains(p))
                        posList.Add(p);
                }
            }

            // Generate each position for the line renders.
            foreach (var position in posList)
            {
                var path = FindMapPath(startPos, position);
                if (path != null)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        var p = path[i];
                        p.next = path[i + 1];

                        if (platformer)
                            if ((i + 1) == (path.Count - 1))
                            {
                                var pathRoute = FindMapPath(new Vector3(path[i + 1].x, path[i + 1].y), startPos);
                                if (pathRoute != null)
                                {
                                    p.next = pathRoute[0];

                                    AddRenderPathPosition(pathRoute);
                                }
                            }

                        var pos = (new Vector3(p.x, p.y), new Vector3(p.next.x, p.next.y));

                        if (!_renderPathTest.Contains(pos))
                            _renderPathTest.Add(pos);
                    }
                }

                if (platformer)
                {
                    var node = GetNodeByTile(new Vector3(position.x, position.y));
                    if (!node.isWalkable)
                        continue;

                    // Left down
                    AddRenderPathDirectionDown(position, 1);

                    // Right down
                    AddRenderPathDirectionDown(position, -1);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Basically scene view editor only possible paths for the player.
            if (renderPaths && Application.isEditor && !Application.isPlaying)
            {
                if (tiledNodes != null)
                {
                    for (int i = 0; i < _renderPathTest.Count; i++)
                    {
                        var path = _renderPathTest[i];

                        if (map.ShouldNotFrustumCull(path.start) || map.ShouldNotFrustumCull(path.end))
                            Debug.DrawLine(path.start, path.end, Color.black);
                    }
                }

            }
            else if (!renderPaths && Application.isEditor && _renderPathTest != null)
            {
                tiledNodes = null;
                _renderPathTest = null;
            }

            if (tiledNodes != null)
                foreach (var tile in tiledNodes)
                {
                    Vector3 pos = new Vector3(tile.Value.x, tile.Value.y);
                    
                    if (map.ShouldNotFrustumCull(pos))
                        tile.Value.DrawGizmos(pos);
                }
        }
    }
#endif
}
