using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Tile : MonoBehaviour
    {
        public Node node;
        public Cover tileCover;
        public enum ObstructionType
        {
            None,
            SomethingToBeAdded
        };
        public ObstructionType obstruction;
        public GameObject obstructionOBJ;

        public LayerMask obstructionMask;

        Renderer r;

        public GameObject characterToSpawn;

        public delegate void OnTileAction();
        public OnTileAction onTileAction;

        public static bool draw;
        private void Start()
        {
            r = GetComponent<Renderer>();
        }

        [ContextMenu("Toggle Gizmo")]
        public void ToggleDraw()
        {
            draw = !draw;
        }

        private void OnDrawGizmos()
        {
            if (!draw)
            {
                return;
            }
            Gizmos.color = Color.black;
            if (tileCover.coverDirections.Count == 0 || !tileCover.enabled)
            {
                return;
            }
            foreach (Cover.Direction d in tileCover.coverDirections)
            {
                switch (d)
                {
                    case Cover.Direction.Up:
                        
                        break;
                    case Cover.Direction.Down:
                        break;
                    case Cover.Direction.Left:
                        Gizmos.DrawWireCube(transform.position + Vector3.left * 0.5f + Vector3.up * 1, new Vector3(0.1f, 1, 0.85f));
                        break;
                    case Cover.Direction.Right:
                        Gizmos.DrawWireCube(transform.position + Vector3.right * 0.5f + Vector3.up * 1, new Vector3(0.1f, 1, 0.85f));
                        break;
                    case Cover.Direction.Forward:
                        Gizmos.DrawWireCube(transform.position + Vector3.forward * 0.5f + Vector3.up * 1, new Vector3(0.85f, 1, 0.1f));
                        break;
                    case Cover.Direction.Backward:
                        Gizmos.DrawWireCube(transform.position + Vector3.back * 0.5f + Vector3.up * 1, new Vector3(0.85f, 1, 0.1f));
                        break;
                }
            }
        }

        [ContextMenu("Get Obstruction")]
        public void GetObstruction ()
        {
            Collider[] hits = Physics.OverlapBox(transform.position + Vector3.up, Vector3.one * 0.25f, Quaternion.identity, obstructionMask);
            if (hits.Length > 1)
            {
                Debug.LogError("Reduce extents or check tile: Too many obstructions");
                return;
            }
            obstructionOBJ = hits[0].gameObject;
            obstructionOBJ.transform.parent = this.transform;
            obstruction = ObstructionType.SomethingToBeAdded;
        }

        public bool Obstructed
        {
            get
            {
                if (obstruction != ObstructionType.None)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
        }

        /*
        private void OnMouseEnter()
        {
            if (!Obstructed)
            {
                Game_Controller.controller.MOTile(this);
            }
        }

        private void OnMouseOver()
        {
            if (!Obstructed)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Game_Controller.controller.MDTile(this);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    Game_Controller.controller.MDRTile(this);
                }
                
            }
        }
        */
        public void UnSelectTile ()
        {
            r.material.SetInt("_Selected", 0);
        }

        public void TileAction ()
        {
            if (onTileAction != null)
            {
                onTileAction();
            }
            
        }
        
    }
}

