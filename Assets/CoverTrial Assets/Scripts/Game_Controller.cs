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
        public List<Character> allEnemies = new List<Character>();
        public List<Character> allAllies = new List<Character>();
        public GameObject tileContainer;

        public float edgeDistance;

        public bool useVisualisation;
        public int visCount, visCountTarget;

        public GameObject selectionCylinder, characterContainer;

        public Tile selectedTile;

        public bool feed;

        public bool playerTurn = true;

        public LayerMask obstructionMask, interactionMask;

        public bool showGrid;
        private void Start()
        {
            controller = this;
            if (feed)
            {
                GetTiles();
            }
        }

        private void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200, interactionMask))
            {
                selectionCylinder.transform.position = hit.transform.position;

                if (Input.GetMouseButtonDown(0))
                {
                    Tile t = hit.transform.GetComponent<Tile>();
                    if (t != null)
                    {
                        MDTile(t);
                    }
                    else
                    {
                        Character c = hit.transform.GetComponent<Character>();
                        if (c != null)
                        {
                            MDCharacter(c);
                        }
                        else
                        {
                            print(hit.transform.name);
                        }
                    }
                } 
                else if (Input.GetMouseButtonDown(1))
                {
                    Tile t = hit.transform.GetComponent<Tile>();
                    if (t != null)
                    {
                        MDRTile(t);
                    }
                }

            }

            if (Input.GetKeyDown(KeyCode.C) && selectedTile != null)
            {
                ToggleCrouch();
            }
        }

        #region Generation

        [ContextMenu("Wipe Grid")]
        public void WipeGrid()
        {
            allTiles.Clear();
            allNodes.Clear();
            allEdges.Clear();
            foreach (Character c in allEnemies)
            {
                if (c != null)
                {
                    Destroy(c.gameObject);
                }
            }
            allEnemies.Clear();
            foreach (Character c in allAllies)
            {
                if (c != null)
                {
                    Destroy(c.gameObject);
                }
            }
            allAllies.Clear();
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
                    position = t.transform.position + t.transform.up,
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
                        Edge newEdge = new Edge()
                        {
                            endNode = n,
                            door = new Door()
                        };
                        node.edges.Add(newEdge);
                        allEdges.Add(newEdge);
                    }
                }
            }

            foreach (Node node in allNodes)
            {
                foreach (Edge edge in node.edges)
                {
                    Ray ray = new Ray(node.position, edge.endNode.position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 1.2f, obstructionMask))
                    {
                        Obstruction ob = hit.transform.GetComponent<Obstruction>();
                        if (ob.type == Obstruction.ObType.Door)
                        {
                            edge.door = ob.door;
                        }
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
                    t.node.occupant.currentTile = t;
                    if (t.node.occupant.faction == Character.Faction.Enemy)
                    {
                        allEnemies.Add(t.node.occupant);
                    }
                    else if (t.node.occupant.faction == Character.Faction.Player)
                    {
                        allAllies.Add(t.node.occupant);
                    }
                }
            }
            yield break;
        }

        public List<Node> GeneratePath (Node start, Node end)
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
                    if (edge.door.enabled)
                    {
                        if (!edge.endNode.obstructed && !edge.door.locked)
                        {
                            neighbours.Add(edge.endNode);
                        }
                    }
                    else
                    {
                        if (!edge.endNode.obstructed)
                        {
                            neighbours.Add(edge.endNode);
                        }
                    }


                }
                foreach (Node neighbour in neighbours)
                {
                    if (closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    if (neighbour != end && neighbour.occupant != null)
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

        #endregion

        #region Mouse Inputs
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
            if (!playerTurn)
            {
                return;
            }
            if (selectedTile != null && selectedTile.node.occupant != null)
            {
                float enemyWait = 0;
                List<Node> path = GeneratePath(selectedTile.node, t.node);
                if (path != null)
                {
                    Character ch = selectedTile.node.occupant;
                    //print("Start: " + selectedTile.node.position.ToString() + " | End: " + t.node.position.ToString() + " | PStep One: " + path[0].position.ToString());
                    if (!ch.Crouching)
                    {
                        if (path.Count >= ch.movePoints)
                        {
                            List<Node> pathRange = path.GetRange(0, ch.movePoints);
                            ch.MoveTo(pathRange);
                            enemyWait = (1 / ch.movementSpeed) * pathRange.Count;
                        }
                        else
                        {
                            List<Node> pathRange = path.GetRange(0, path.Count);
                            ch.MoveTo(pathRange);
                            enemyWait = (1 / ch.movementSpeed) * pathRange.Count;
                        }
                    }
                    else
                    {
                        if (path.Count >= ch.crouchMovePoints)
                        {
                            List<Node> pathRange = path.GetRange(0, ch.crouchMovePoints);
                            ch.MoveTo(pathRange);
                            enemyWait = (1 / ch.movementSpeed) * pathRange.Count;
                        }
                        else
                        {
                            List<Node> pathRange = path.GetRange(0, path.Count);
                            ch.MoveTo(pathRange);
                            enemyWait = (1 / ch.movementSpeed) * pathRange.Count;
                        }
                    }
                    
                    
                    //path[path.Count - 1].occupant = selectedTile.node.occupant;
                    selectedTile.node.occupant = null;
                    //MDTile(GetTileFromNode(path[0]));

                    StartCoroutine(EnemyTurn(enemyWait));
                }
            }
        }

        public void ToggleCrouch ()
        {
            
            if (!playerTurn)
            {
                return;
            }

            if (selectedTile != null && selectedTile.node.occupant != null)
            {
                selectedTile.node.occupant.Crouching = !selectedTile.node.occupant.Crouching;
            }
        }

        public void MDCharacter (Character character)
        {
            MDTile(character.currentTile);
        }
#endregion

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

        public Node GetNodeFromWorldPos (Vector3 pos)
        {
            Node result = null;
            float bigDist = Mathf.Infinity;
            foreach (Node node in allNodes)
            {
                float dist = Vector3.Distance(pos, node.position);
                if (dist < bigDist)
                {
                    bigDist = dist;
                    result = node;
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

        IEnumerator EnemyTurn (float wait)
        {
            playerTurn = false;
            //Do logic
            yield return new WaitForSeconds(wait);
            foreach (Character ch in allEnemies)
            {
                ch.StartTurn();
                while (!ch.turnFinished)
                {
                    yield return null;
                }
            }
            yield return null;

            playerTurn = true;

            yield break;
        }

        private void OnDrawGizmos()
        {
            if (!showGrid)
            {
                return;
            }
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
                    else if (edge.door.enabled)
                    {
                        Gizmos.color = Color.yellow;
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
        public Color openColor, closedColor, lockedColor;
        public bool Open
        {
            get
            {
                return open;
            }

            set
            {
                open = value;
                if (prefab != null)
                {
                    if (value)
                    {
                        prefab.GetComponent<Renderer>().material.color = openColor;
                    }
                    else
                    {
                        prefab.GetComponent<Renderer>().material.color = closedColor;
                    }
                }
                
                
            }
        }

        public bool Locked
        {
            get
            {
                return locked;
            }

            set
            {
                locked = value;
                if (value)
                {
                    Open = false;
                    prefab.GetComponent<Renderer>().material.color = lockedColor;
                } 
                else
                {
                    Open = Open;
                }
            }
        }
    }
}

