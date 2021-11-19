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

        public Dictionary<Vector3, Tile> loadedTiles;

#if UNITY_EDITOR
        [SerializeField] private bool _editMode;
#endif

        public TileData NextTile(TileData a_tileData, bool a_next)
        {
            if (a_tileData.prefabIndex == mapInfo.prefabs.Length)
                return a_tileData;

            if (a_next)
            {
                a_tileData.prefabIndex++;

                if (a_tileData.prefabIndex >= mapInfo.prefabs.Length)
                    a_tileData.prefabIndex = 0;
            }
            else
            {
                a_tileData.prefabIndex--;

                if (a_tileData.prefabIndex < 0)
                    a_tileData.prefabIndex = mapInfo.prefabs.Length - 1;
            }

            RemoveTileAtPosition(a_tileData.position);

            CreateTile(a_tileData, true);

            return a_tileData;
        }

        public GameObject CreateTile(TileData a_tile, bool a_assign = false)
        {
            if (a_tile.prefabIndex >= mapInfo.prefabs.Length || a_tile.prefabIndex == -1 || GetTileAt(a_tile.position) != null)
                return null;

            var obj = Instantiate(mapInfo.prefabs[a_tile.prefabIndex], a_tile.position, default, transform);
            obj.tag = "Tile";
            obj.gameObject.name = $"Tile <{a_tile.position.x}, {a_tile.position.y}>";

            if (obj.transform.TryGetComponent(out Tile t))
            {
                onTileChange?.Invoke(t);

                t.position = a_tile.position;
                t.data = a_tile;

                SetTileAtPosition(t, a_tile.position, a_assign);
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

            mapInfo.RemoveTile(a_position);

            loadedTiles.Remove(a_position);

            DestroyImmediate(tile.gameObject);
        }

        public void LoadSavedTiles()
        {
            loadedTiles = new Dictionary<Vector3, Tile>();

            // When a child gets destroyed, it restates the array, so you have to loop backwards.
            for (int i = transform.childCount; i > 0; --i)
                DestroyImmediate(transform.GetChild(0).gameObject);

            if (mapInfo.tiles != null)
                for (int i = 0; i < mapInfo.tiles.Length; i++)
                {
                    var tile = mapInfo.tiles[i];

                    if (mapInfo.prefabs.Length < tile.prefabIndex)
                        continue;

                    tile.position = transform.TransformPoint(tile.localPosition);

                    mapInfo.tiles[i] = tile;

                    CreateTile(tile);
                }
        }

        public void SetTileAtPosition(Tile a_tile, Vector3 a_position, bool a_setTile = false)
        {
            if (a_setTile)
                mapInfo.SetTile(mapInfo.tiles.Length, new TileData { position = a_position, localPosition = a_tile.transform.localPosition, prefabIndex = a_tile.data.prefabIndex });

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
            return loadedTiles.ContainsKey(a_position) ? loadedTiles[a_position] : null;
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
                        rT = loadedTiles.ContainsKey(position) ? loadedTiles[position].data : new TileData { prefabIndex = -2, position = position };
                    }
                }
            }

            return rT;
        }

        private void Start()
        {
            LoadSavedTiles();
        }

#if UNITY_EDITOR
        private TileData _currentHover;
        private Color _hoverYellow = new Color(1, 1, 0.2f, 0.5f);
        private Color _hoverRed = new Color(1, 0, 0f, 0.5f);

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

            if (_currentHover.prefabIndex != -1 && _editMode)
            {
                Gizmos.color = _currentHover.prefabIndex > -1 ? _hoverRed : _hoverYellow;
                Gizmos.DrawCube(_currentHover.position, cellSize);
            }
        }
#endif
    }
}
