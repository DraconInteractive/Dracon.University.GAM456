using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //Pickup key: Open door
    public class Key : TileAddon
    {
        //Reference to the door. The GO not the data, as the data copies, not references. 
        public GameObject door;

        // Update is called once per frame
        void Update()
        {
            //pretty rotations. Arbitrary
            transform.Rotate(Vector3.up, 45 * Time.deltaTime);
        }

        public override void TileAction ()
        {
            //Get the main controller. Search for the door. Set the door to unlocked, then tell game ui to tell the player. Perform base tile action afterward. 
            Game_Controller controller = Game_Controller.controller;

            foreach (Node node in controller.allNodes)
            {
                foreach (Edge edge in node.edges)
                {
                    if (edge.door.enabled && edge.door.prefab == door)
                    {
                        edge.door.Locked = false;
                        UIController.controller.AddTextToContainerQueue("Door Unlocked");
                    }
                }
            }

            base.TileAction();
        }
    }
}

