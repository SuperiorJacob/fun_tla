using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.MapCreation
{
    [System.Serializable]
    public struct GridData
    {
        public Vector3 gridSize;
        public Vector2 cellSize;
    }

    [System.Serializable]
    public struct TileData
    {
        public Vector3 position;
        public int prefabIndex;
    }

    [System.Serializable]
    public enum GridAnchor
    {
        TopLeft = 0,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [System.Serializable]
    public struct MapInfo
    {
        [Header("Grid")]
        public GridAnchor gridAnchor;
        public GridData grid;

        [Header("Tiles"), NonReorderable]
        public GameObject[] prefabs;

        [NonReorderable]
        public TileData[] tiles;

        public int SetTile(int a_index, TileData a_data)
        {
            TileData[] data;

            if (tiles.Length < (a_index + 1))
            {
                data = new TileData[a_index + 1];
            }
            else
                data = new TileData[tiles.Length];

            tiles.CopyTo(data, 0);

            data[a_index] = a_data;

            tiles = data;

            return a_index;
        }

        public void RemoveTile(Vector3 a_position)
        {
            TileData[] data = new TileData[tiles.Length - 1];

            int count = 0;
            foreach (var tile in tiles)
            {
                if (tile.position == a_position)
                    continue;

                data[count] = tile;
                count++;
            }

            tiles = data;
        }

        public Vector3 GetGridPosition(Vector3 a_origin, Vector3 a_cellSize, int a_x, int a_y)
        {
            Vector3 cellSize = a_cellSize;

            switch (gridAnchor)
            {
                case GridAnchor.TopLeft:
                    return new Vector3(a_origin.x + cellSize.x * a_x, a_origin.y - cellSize.y * a_y, 0);

                case GridAnchor.TopRight:
                    return new Vector3(a_origin.x - cellSize.x * a_x, a_origin.y - cellSize.y * a_y, 0);

                case GridAnchor.BottomLeft:
                    return new Vector3(a_origin.x + cellSize.x * a_x, a_origin.y + cellSize.y * a_y, 0);

                case GridAnchor.BottomRight:
                    return new Vector3(a_origin.x - cellSize.x * a_x, a_origin.y + cellSize.y * a_y, 0);

                default:
                    return Vector3.zero;
            }
        }
    }

    public class MapData : ScriptableObject
    {
        public MapInfo mapInfo;
    }
}
