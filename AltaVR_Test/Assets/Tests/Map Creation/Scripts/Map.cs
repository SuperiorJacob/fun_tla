using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AltaVR.MapCreation
{
    [SelectionBase]
    public class Map : MonoBehaviour
    {
        public MapData mapData;

        public MapInfo mapInfo;

        [Header("Events")]
        public UnityEvent<Tile> onTileChange;

        public Dictionary<Vector3, Tile> loadedTiles = new Dictionary<Vector3, Tile>();

        public void TileShuffled(Tile a_tile)
        {

        }

        private void Start()
        {
            if (mapData == null)
                return;

            //var cellCount = GetGridCount();

            //for (int x = 0; x < cellCount.x; x++)
            //{
            //    for (int y = 0; y < cellCount.y; y++)
            //    {
            //        int index = Random.Range(0, mapInfo.prefabs.Length);

            //        var tilePrefab = GetTilePrefab(index);

            //        if (tilePrefab == null)
            //            continue;

            //        Vector3 position = GetGridPosition(x, y);

            //        var obj = Instantiate(tilePrefab, position, default, transform);
            //        obj.tag = "Tile";
            //        obj.gameObject.name = $"Tile <{x}, {y}>";

            //        if (obj.TryGetComponent(out Tile tile))
            //        {
            //            tile.position = position;
            //            tiles[position] = tile;
            //        }
            //    }
            //}
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public GameObject CreateTile(TileData a_tile, bool a_assign = false)
        {
            if (a_tile.prefabIndex == -1)
                return null;

            var obj = Instantiate(mapInfo.prefabs[a_tile.prefabIndex], a_tile.position, default, transform);
            obj.tag = "Tile";
            obj.gameObject.name = $"Tile <{a_tile.position.x}, {a_tile.position.y}>";

            if (obj.transform.TryGetComponent(out Tile t))
            {
                t.position = a_tile.position;
                t.data = a_tile;

                if (a_assign)
                    SetTileAtPosition(t, a_tile.position);
            }

            return obj;
        }

        public void RemoveTileAtPosition(Vector3 a_position)
        {
            if (!loadedTiles.ContainsKey(a_position))
                return;

            var tile = loadedTiles[a_position];

            if (tile == null)
                return;

            mapInfo.RemoveTile(tile.id);

            loadedTiles.Remove(a_position);

            DestroyImmediate(tile.gameObject);
        }

        public void LoadSavedTiles()
        {
            loadedTiles.Clear();

            // When a child gets destroyed, it restates the array, so you have to loop backwards.
            for (int i = transform.childCount; i > 0; --i)
                DestroyImmediate(transform.GetChild(0).gameObject);

            if (mapInfo.tiles != null)
                foreach (var tile in mapInfo.tiles)
                {
                    if (mapInfo.prefabs.Length < tile.prefabIndex)
                        continue;

                    CreateTile(tile);
                }
        }

        public void SetTileAtPosition(Tile a_tile, Vector3 a_position)
        {
            mapInfo.SetTile(mapInfo.tiles.Length, new TileData { position = a_position, prefabIndex = a_tile.id });

            loadedTiles[a_position] = a_tile;
        }

        public GameObject GetTilePrefab(int a_index)
        {
            if (mapInfo.prefabs.Length == 0 || mapInfo.prefabs.Length < a_index || a_index < 0)
                return null;
            else
                return mapInfo.prefabs[a_index];
        }

        public Tile GetTileAt(Vector3 a_position)
        {
            return loadedTiles[a_position];
        }

        public Vector3 GetCellSize()
        {
            int gridCellX = (int)Mathf.Clamp(mapInfo.grid.cellSize.x, 0, mapInfo.grid.gridSize.x);
            int gridCellY = (int)Mathf.Clamp(mapInfo.grid.cellSize.y, 0, mapInfo.grid.gridSize.y);

            return new Vector3(gridCellX, gridCellY);
        }

        public Vector2Int GetGridCount()
        {
            Vector3 cell = GetCellSize();

            int gridCellCount_x = (int)(mapInfo.grid.gridSize.x / cell.x);
            int gridCellCount_y = (int)(mapInfo.grid.gridSize.y / cell.y);

            return new Vector2Int(gridCellCount_x, gridCellCount_y);
        }

        public Vector3 GetGridPosition(int a_x, int a_y)
        {
            return mapInfo.GetGridPosition(transform.position, GetCellSize(), a_x, a_y);
        }

        public TileData GetTileByClosestPosition(Vector3 a_position)
        {
            TileData rT = new TileData { prefabIndex = -1 };

            Vector3 cellSize = GetCellSize();
            Vector2Int cellCount = GetGridCount();

            cellSize /= 2;

            for (int x = 0; x < cellCount.x; x++)
            {
                for (int y = 0; y < cellCount.y; y++)
                {
                    Vector3 position = GetGridPosition(x, y);

                    if (a_position.x < (position.x + cellSize.x) && a_position.x > (position.x - cellSize.x)
                        && a_position.y < (position.y + cellSize.y) && a_position.y > (position.y - cellSize.y))
                    {
                        Vector3 newPos = -position;
                        newPos.x = position.x;

                        rT = loadedTiles.ContainsKey(newPos) ? loadedTiles[newPos].data : new TileData { prefabIndex = -2, position = newPos };
                    }
                }
            }

            return rT;
        }

#if UNITY_EDITOR
        private TileData _currentHover;

        public void DrawHovered(TileData a_tile)
        {
            _currentHover = a_tile;
        }

        private void OnDrawGizmos()
        {
            if (mapData == null)
                return;

            // Drawing the grid.
            Gizmos.color = Color.yellow;

            if (mapInfo.grid.cellSize.x == 0 || mapInfo.grid.cellSize.y == 0)
                return;

            Vector2Int cellCount = GetGridCount();
            Vector3 cellSize = GetCellSize();

            for (int x = 0; x < cellCount.x; x++)
            {
                for (int y = 0; y < cellCount.y; y++)
                {
                    Gizmos.DrawWireCube(GetGridPosition(x, y), cellSize);
                }
            }

            if (_currentHover.prefabIndex != -1)
            {
                if (_currentHover.prefabIndex > -1)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(_currentHover.position, cellSize);
                }
                else
                    Gizmos.DrawCube(_currentHover.position, cellSize);
            }
        }
#endif
    }
}
