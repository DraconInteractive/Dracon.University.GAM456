using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class TileAddon : MonoBehaviour
    {
        public Tile thisTile;
        public bool destroyOnActivate;
        // Use this for initialization
        void Start()
        {
            //Literally THE MOST UNEFFICIENT LINE I HAVE EVER WRITTEN. Lol
            Invoke("AssignToTile", 0.1f);
        }
        public void AssignToTile()
        {
            thisTile = Game_Controller.controller.GetTileFromNode(Game_Controller.controller.GetNodeFromWorldPos(transform.position));
            thisTile.onTileAction += TileAction;
        }

        public virtual void TileAction()
        {
            if (destroyOnActivate)
            {
                thisTile.onTileAction -= TileAction;
                Destroy(this.gameObject);
            }
        }
    }
}

