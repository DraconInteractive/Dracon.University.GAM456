using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class DoorOpener : TileAddon
    {
        public GameObject door;

        // Update is called once per frame
        public override void TileAction()
        {
            
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

