using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //another tile extension. Put in front of a door, so that a closed door will open when a player attempts to go through it. 
    public class DoorOpener : TileAddon
    {
        public GameObject door;

        // Update is called once per frame
        public override void TileAction()
        {
            //Find the door (in a horribly inefficient way), then open it. Tell the ui to tell the player. Then do basic tile stuff. 
            Game_Controller controller = Game_Controller.controller;
            foreach (Node node in controller.allNodes)
            {
                foreach (Edge edge in node.edges)
                {
                    if (edge.door.enabled && edge.door.prefab == door)
                    {
                        if (!edge.door.locked && !edge.door.open)
                        {
                            edge.door.Open = true;
                            UIController.controller.AddTextToContainerQueue("Door Opened");
                        }
                    }
                }
            }

            base.TileAction();
        }
    }
}

