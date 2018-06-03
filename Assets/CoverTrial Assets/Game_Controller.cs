using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    public class Game_Controller : MonoBehaviour
    {
        public static Game_Controller controller;
        public List<Tile> allTiles = new List<Tile>();
        public List<Node> allNodes = new List<Node>();
        public List<Edge> allEdges = new List<Edge>();
        public GameObject tileContainer;

        public float edgeDistance;

        public bool useVisualisation;
        public int visCount, visCountTarget;

        public GameObject selectionCylinder, characterContainer;

        public Tile selectedTile;

        public bool feed;
        private void Start()
        {
            controller = this;
            if (feed)
            {
                GetTiles();
            }

        }
        [ContextMenu("Wipe Grid")]
        public void WipeGrid()
        {
            allTiles.Clear();
            allNodes.Clear();
            allEdges.Clear();
        }

        [ContextMenu("Get Tiles")]
        public void GetTiles ()
        {
            Tile[] tiles = tileContainer.GetComponentsInChildren<Tile>();
            foreach (Tile t in tiles)
            {
                if (!allTiles.Contains(t))
                {
                    allTiles.Add(t);
                }
            }

            if (feed)
            {
                StartNodeGeneration();
            }
        }

        [ContextMenu("Generate Nodes")]
        public void StartNodeGeneration ()
        {
            allNodes.Clear();
            StartCoroutine(GenerateNodes());
        }

        IEnumerator GenerateNodes ()
        {
            print("Generating Nodes");
            foreach (Tile t in allTiles)
            {
                Node newNode = new Node()
                {
                    position = t.transform.position + Vector3.up,
                    nodeCover = t.tileCover,
                    obstructed = t.Obstructed
                };
                t.node = newNode;
                allNodes.Add(newNode);

                

                if (useVisualisation)
                {
                    visCount++;
                    if (visCount >= visCountTarget)
                    {
                        visCount = 0;
                        yield return null;
                    }
                }
            }

            if (feed)
            {
                StartEdgeGeneration();
            }
            yield break;
        }

        [ContextMenu("Generate Edges")]
        public void StartEdgeGeneration ()
        {
            StartCoroutine(GenerateEdges());
        }

        IEnumerator GenerateEdges ()
        {
            print("Generating Edges");
            foreach (Node node in allNodes)
            {
                node.edges.Clear();
                foreach (Node n in allNodes)
                {
                    float dist = Vector3.Distance(node.position, n.position);
                    if (dist < edgeDistance && n != node)
                    {
                        node.edges.Add(new Edge()
                        {
                            endNode = n,
                            door = new Door()
                            {
                                enabled = false,
                                open = false,
                                locked = false,
                                prefab = null
                            }
                        });
                    }
                }
            }

            if (feed)
            {
                StartSpawnCharacters();
            }
            yield break;
        }

        [ContextMenu("Spawn Characters")]
        public void StartSpawnCharacters ()
        {
            StartCoroutine(SpawnCharacters());
        }

        IEnumerator SpawnCharacters ()
        {
            print("Starting Character Spawn");
            foreach (Tile t in allTiles)
            {
                if (t.characterToSpawn != null)
                {
                    t.node.occupant = Instantiate(t.characterToSpawn, t.node.position, Quaternion.identity, characterContainer.transform).GetComponent<Character>();
                }
            }
            yield break;
        }

        List<Node> GeneratePath (Node start, Node end)
        {
            List<Node> aNodes = allNodes;
            List<Node> finalPath = new List<Node>();

            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();

            openSet.Add(start);
            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == end)
                {
                    List<Node> path = RetracePath(start, end);
                    return path;
                }

                List<Node> neighbours = new List<Node>();
                foreach (Edge edge in currentNode.edges)
                {
                    if (!edge.endNode.obstructed)
                    {
                        neighbours.Add(edge.endNode);
                    }
                    
                }
                foreach (Node neighbour in neighbours)
                {
                    if (closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, end);

                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
            return null;
        }

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.RoundToInt(Mathf.Abs(nodeA.position.x - nodeB.position.x));
            int dstY = Mathf.RoundToInt(Mathf.Abs(nodeA.position.z - nodeB.position.z));

            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            else
            {
                return 14 * dstX + 10 * (dstY - dstX);
            }
        }

        List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            return(path);
        }

        public void MOTile (Tile t)
        {
            selectionCylinder.transform.position = t.transform.position;
        }

        public void MDTile (Tile t)
        {
            //Shader.SetGlobalFloat("_Selected", 0);
            foreach (Tile tile in allTiles)
            {
                tile.UnSelectTile();
            }
            t.GetComponent<Renderer>().material.SetFloat("_Selected", 1);

            selectedTile = t;
        }

        public void MDRTile (Tile t)
        {
            if (selectedTile != null && selectedTile.node.occupant != null)
            {
                List<Node> path = GeneratePath(selectedTile.node, t.node);
                if (path != null)
                {
                    print("Start: " + selectedTile.node.position.ToString() + " | End: " + t.node.position.ToString() + " | PStep One: " + path[0].position.ToString());
                    selectedTile.node.occupant.MoveTo(path);
                    //path[path.Count - 1].occupant = selectedTile.node.occupant;
                    selectedTile.node.occupant = null;
                    //MDTile(GetTileFromNode(path[0]));
                }
            }
        }
        public void MDCharacter (Character character)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Ray(transform.position, Vector3.down), 3);
            foreach (RaycastHit hit in hits)
            {
                Tile t = hit.transform.GetComponent<Tile>();
                if (t != null)
                {
                    MDTile(t);
                }
            }

        }

        public Tile GetTileFromNode (Node node)
        {
            float bigDist = Mathf.Infinity;
            Tile result = null;
            foreach (Tile t in allTiles)
            {
                float dist = Vector3.Distance(node.position, t.transform.position);
                if (dist < bigDist)
                {
                    bigDist = dist;
                    result = t;
                }
            }

            return result;
        }

        public void SetNodeOccupant (Node newNode, Character character)
        {
            foreach (Node n in allNodes)
            {
                if (n.occupant == character)
                {
                    n.occupant = null;
                    break;
                }
            }

            foreach (Node n in allNodes)
            {
                if (n == newNode)
                {
                    n.occupant = character;
                }
            }
        }
        private void OnDrawGizmos()
        {
            if (allNodes.Count <= 0)
            {
                return;
            }
            foreach (Node n in allNodes)
            {
                if (n.obstructed)
                {
                    Gizmos.color = Color.red;
                } 
                else
                {
                    Gizmos.color = Color.blue;
                }

                Gizmos.DrawWireSphere(n.position, 0.1f);
                foreach (Edge edge in n.edges)
                {
                    if (n.obstructed || edge.endNode.obstructed)
                    {
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.blue;
                    }
                    Gizmos.DrawLine(n.position, edge.endNode.position);
                }
            }
        }
    }

    [System.Serializable]
    public class Node
    {
        public Vector3 position;
        public Cover nodeCover;

        public bool obstructed;

        public List<Edge> edges = new List<Edge>();

        public Character occupant;

        public int gCost, hCost;
        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public Node parent;
    }

    [System.Serializable]
    public class Cover
    {
        public bool enabled;
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        };

        public List<Direction> coverDirections = new List<Direction>();
    }

    [System.Serializable]
    public class Edge
    {
        public Node endNode;
        public Door door;
    }

    [System.Serializable]
    public class Door
    {
        public bool enabled;
        public bool open;
        public bool locked;
        public GameObject prefab;
    }
}

