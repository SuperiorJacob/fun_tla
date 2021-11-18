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

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0f;

                Vector3 currentLocalPos = pathFinder.map.transform.InverseTransformVector(transform.position);

                Vector3 tilePos = pathFinder.map.GetTileByClosestPosition(currentLocalPos).position;

                _currentPath = pathFinder.FindMapPath(tilePos, mouseWorldPos);
                _currentNode = 0;

            }
            else if (_currentPath != null)
            {
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    Vector3 start = new Vector3(_currentPath[i].x, _currentPath[i].y);
                    Vector3 end = new Vector3(_currentPath[i + 1].x, _currentPath[i + 1].y);

                    Debug.DrawLine(start, end, Color.black);
                }

                Vector3 go = new Vector3(_currentPath[_currentNode].x, _currentPath[_currentNode].y);

                // Check distance
                if ((transform.position - go).magnitude < 0.1f && (_currentNode + 1) < _currentPath.Count)
                    _currentNode += 1;

                transform.position = Vector3.Lerp(transform.position, go, Time.deltaTime * _playerSpeed);
            }
        }
    }
}
