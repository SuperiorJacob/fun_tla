using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.MapCreation
{
    public class Tile : MonoBehaviour
    {
        public int id;

        [HideInInspector] public TileData data;
        [HideInInspector] public Vector3 position;
    }
}
