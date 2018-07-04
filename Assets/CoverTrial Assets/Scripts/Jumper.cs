using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //another extension for basic tile stuff. When the player goes to a jumper, it propels them to a different tile. 
    public class Jumper : TileAddon
    {
        //Tile to jump to
        public Tile jumpTo;

        public override void TileAction()
        {
            Game_Controller controller = Game_Controller.controller;
            //create a path from the jumpTo, then tell the player to enact it. Then do basic tile stuff. 
            List<Node> mover = new List<Node>();
            mover.Add(controller.GetNodeFromWorldPos(jumpTo.transform.position));
            thisTile.node.occupant.MoveTo(mover);

            base.TileAction();
        }
    }
}

