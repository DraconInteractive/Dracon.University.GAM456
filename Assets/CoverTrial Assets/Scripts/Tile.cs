using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //Preexisting structure used to generate nodes and other path/nav relevant information. 
    public class Tile : MonoBehaviour
    {
        //The generated node
        public Node node;
        //What cover applies to tile
        public Cover tileCover;
        //Is tile obstructed, and what type?
        public enum ObstructionType
        {
            None,
            //There will probably be multiple types, so yeh.
            SomethingToBeAdded
        };
        public ObstructionType obstruction;
        //GO ref to obstruction
        public GameObject obstructionOBJ;
        //Used for detection obstructions on tile
        public LayerMask obstructionMask;
        //For changing color etc
        Renderer r;
        //Specify character if this is a spawnpoint
        public GameObject characterToSpawn;
        //Delegate called when a character that is the player steps on the tile
        public delegate void OnTileAction();
        public OnTileAction onTileAction;
        //draw gizmos. Static so it applies to all tiles. 
        public static bool draw;
        private void Start()
        {
            r = GetComponent<Renderer>();
        }
        //Self explanatory. 
        [ContextMenu("Toggle Gizmo")]
        public void ToggleDraw()
        {
            draw = !draw;
        }
        //If we are drawing gizmos, check to see if we have cover. If not, go away. 
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
            //If we do, draw a cube for every cover direction that is enabled. 
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
            //If this is a spawnpoint, draw a capsule to symbolise it
            if (characterToSpawn != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(transform.position + Vector3.up * 1f, new Vector3(0.25f, 1, 0.25f));
            }
        }
        //editor tool, gets any obstruction sitting on the tile by doing a quick overlap box. 
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

        //used to set the shader for the tile selection
        public void UnSelectTile ()
        {
            r.material.SetInt("_Selected", 0);
        }
        //Do the delegate. 
        public void TileAction ()
        {
            if (onTileAction != null)
            {
                onTileAction();
            }
            
        }
        
    }
}

