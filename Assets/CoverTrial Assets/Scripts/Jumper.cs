using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Jumper : TileAddon
    {
        public Tile jumpTo;

        public override void TileAction()
        {
            Game_Controller controller = Game_Controller.controller;

            List<Node> mover = new List<Node>();
            mover.Add(controller.GetNodeFromWorldPos(jumpTo.transform.position));
            thisTile.node.occupant.MoveTo(mover);

            base.TileAction();
        }
    }
}

