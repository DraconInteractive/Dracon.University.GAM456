using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //base class for a tile functionality extender. 
    public class TileAddon : MonoBehaviour
    {
        public Tile thisTile;
        public bool destroyOnActivate;
        // Use this for initialization
        void Start()
        {
            
            Invoke("AssignToTile", 0.1f);
        }
        public void AssignToTile()
        {
            //set the tile, and then add this tiles function to the delegate for tile actions. 
            //...needs optimsation
             thisTile = Game_Controller.controller.GetTileFromNode(Game_Controller.controller.GetNodeFromWorldPos(transform.position));
            thisTile.onTileAction += TileAction;
        }

        public virtual void TileAction()
        {
            //Destroy this if needed, (if its a pickup for example), and unsubscribe from the event. 
            if (destroyOnActivate)
            {
                thisTile.onTileAction -= TileAction;
                Destroy(this.gameObject);
            }
        }
    }
}

