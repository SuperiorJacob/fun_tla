using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AltaVR.MapCreation
{
    public class ClickTile : MonoBehaviour
    {
        public Map map;

        public void LeftClick()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.transform.tag == "Tile" && hit.transform.TryGetComponent(out Tile tile))
                map.TileShuffled(tile);

        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
                LeftClick();
        }
    }
}
