using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.Pathfinding
{
    public class PlayerFinder : MonoBehaviour
    {
        public Pathfinding pathFinder;
        [SerializeField] private float _playerSpeed = 10f;

        private List<PathNode> _currentPath;
        private int _currentNode = 0;

        private Vector3 CalculatePositionOffset(PathNode a_node)
        {
            if (pathFinder.platformer)
            {
                if (!a_node.isWalkable)
                {
                    Vector3 cell = pathFinder.map.GetCellSize();

                    return new Vector3(a_node.x + cell.x, a_node.y + cell.y);
                }
            }
            
            return new Vector3(a_node.x, a_node.y);
        }

        public Vector3 GetPos()
        {
            Vector3 currentLocalPos = pathFinder.map.transform.InverseTransformVector(transform.position);

            Vector3 tilePos = pathFinder.map.GetTileByClosestPosition(currentLocalPos).position;

            return tilePos;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0f;

                _currentPath = pathFinder.FindMapPath(GetPos(), mouseWorldPos);
                _currentNode = 0;

            }
            else if (_currentPath != null)
            {
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    Vector3 start = CalculatePositionOffset(_currentPath[i]);
                    Vector3 end = CalculatePositionOffset(_currentPath[i + 1]);

                    Debug.DrawLine(start, end, Color.black);
                }

                Vector3 go = CalculatePositionOffset(_currentPath[_currentNode]);

                // Check distance
                if ((transform.position - go).magnitude < 0.1f && (_currentNode + 1) < _currentPath.Count)
                    _currentNode += 1;

                transform.position = Vector3.Lerp(transform.position, go, Time.deltaTime * _playerSpeed);
            }
        }
    }
}
