using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Key : TileAddon
    {
        public GameObject door;

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.up, 45 * Time.deltaTime);
        }

        public override void TileAction ()
        {
            
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

