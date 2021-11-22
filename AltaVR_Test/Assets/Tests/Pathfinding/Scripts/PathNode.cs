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
}