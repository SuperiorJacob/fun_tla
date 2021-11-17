using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.MapCreation
{
    public class Tile : MonoBehaviour
    {
        [HideInInspector] public TileData data;
        [HideInInspector] public Vector3 position;
    }
}
