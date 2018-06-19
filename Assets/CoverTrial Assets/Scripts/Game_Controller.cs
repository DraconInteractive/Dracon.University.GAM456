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

        public LayerMask obstructionMask, interactionMask, wallMask, smoothMask;

        public bool showGrid;

        public bool smoothPath = false;
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
            /*
            RaycastHit wallHit;
            if (Physics.Raycast(ray, out wallHit, 200, wallMask))
            {
                
            }
            */

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
                List<Edge> r = new List<Edge>();
                foreach (Edge edge in node.edges)
                {
                    Ray ray = new Ray(node.position, edge.endNode.position - node.position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 1.2f, obstructionMask))
                    {
                        Obstruction ob = hit.transform.GetComponent<Obstruction>();
                        if (ob != null && ob.type == Obstruction.ObType.Door)
                        {
                            edge.door = ob.door;
                        }
                    }
                    Ray smoothRay = new Ray(node.position, edge.endNode.position - node.position);
                    Debug.DrawRay(node.position, edge.endNode.position - node.position, Color.yellow, 5);
                    RaycastHit smoothHit;
                    if (Physics.Raycast(smoothRay, out smoothHit, 1.0f, wallMask))
                    {
                        r.Add(edge);
                    }
                }

                foreach (Edge e in r)
                {
                    node.edges.Remove(e);
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
                    if (smoothPath && path != null && path.Count > 2)
                    {
                        path = SmoothPath(path);
                    }
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

        public List<Node> SmoothPath(List<Node> original)
        {
            List<Node> smoothPath = new List<Node>(original);
            print("Smooth begun: " + smoothPath.Count);
            Node checkPoint = smoothPath[0];
            Node currentPoint = smoothPath[1];
            while (currentPoint != null)
            {
                Ray ray = new Ray(checkPoint.position, (currentPoint.position - checkPoint.position).normalized);
                RaycastHit hit;
                if (Physics.SphereCast(ray, 1f, out hit, Vector3.Distance(checkPoint.position, currentPoint.position), smoothMask))
                {
                    checkPoint = currentPoint;
                    if ((smoothPath.IndexOf(currentPoint) + 1) < smoothPath.Count)
                    {
                        currentPoint = smoothPath[smoothPath.IndexOf(currentPoint) + 1];
                    }
                    else
                    {
                        currentPoint = null;
                    }


                }
                else
                {
                    Node temp = currentPoint;
                    if ((smoothPath.IndexOf(currentPoint) + 1) < smoothPath.Count)
                    {
                        currentPoint = smoothPath[smoothPath.IndexOf(currentPoint) + 1];
                    }
                    else
                    {
                        currentPoint = null;
                    }
                    smoothPath.Remove(temp);
                    
                }
            }
            print("Smooth Finished: " + smoothPath.Count);
            return smoothPath;
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
                if (t.node.occupant != null && t != selectedTile)
                {
                    float enemyWait = 0;
                    List<Node> path = GeneratePath(selectedTile.node, t.node);
                    if (path != null)
                    {
                        Character origin = selectedTile.node.occupant;
                        Character target = t.node.occupant;

                        if (path.Count < origin.attackStepLimit)
                        {
                            target.Damage(origin.attackDamage, origin);
                        }
                        else
                        {
                            path.Remove(path[path.Count - 1]);
                            origin.MoveTo(path);
                        }
                        StartCoroutine(EnemyTurn(enemyWait));
                    }
                }
                else
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
            if (character == null)
            {
                return;
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

    //[System.Serializable]
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

        
        #region deprecated
        List<Node> visibilityChecks = new List<Node>();

        public void GenerateVisibility ()
        {
            //Iterate through nearby nodes. For every node that isnt obstructed, add it to the 'visible' queue. Obstructed nodes will not be branched from. Thus, any node on here has an unobstructed view of Player. 
            //For script to check visibility of node, they simply have to ask if the node in question is contained within the 'visible' nodes list. Instead of containing a bool, the mere existence of the node in the list is proof, improving optimisation. 
            visibilityChecks.Clear();
            foreach (Edge edge in edges)
            {
                if (!edge.endNode.obstructed)
                {
                    visibilityChecks.Add(edge.endNode);
                    foreach (Edge e in edge.endNode.edges)
                    {
                        if (!e.endNode.obstructed)
                        {
                            visibilityChecks.Add(e.endNode);
                        }
                        //stopping here, 2 tiles of visibility away. Should I want further, its viable but for the instance of this I dont really want to (I implemented my own visibilty system earlier)
                    }
                }
                
            }

            //I'd like to note, for the turn-based rpg strategy that I have made for this assignment, I found it much more optimal to use simple raycasts from enemy to player. 
            //While probably not sustainable in a realtime game, in this genre there is only one raycast every few seconds, at most. Much better than storing oodles of visibility nodes/angles. 

            /*
            public bool CanSeeTarget(Character target)
            {
                bool canSeeTarget = false;
                float distToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (distToTarget > sightDist)
                {
                    blocked = "Dist greater than sight";
                    return false;
                }
                Vector3 playerMod = target.transform.position + target.transform.up * target.headOffset;
                Ray ray = new Ray(transform.position + transform.up * headOffset, playerMod - (transform.position + transform.up * headOffset));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, sightDist, sightMask))
                {
                    if (hit.transform.tag == "Player")
                    {
                        canSeeTarget = true;
                    }
                    else
                    {
                        canSeeTarget = false;
                    }
                }
                else
                {
                    canSeeTarget = false;
                }

                return canSeeTarget;
            }
            */

            //Also, I have left out angles due to my cover system. The cover system is stored per node (made more efficient by sharing data from cover with visibility). 
            //The cover system stores all side angles, as well as up and down (essentially 6DOF). 
            //Should the player wish to find out if the player is visible from a node, they get the angle from their node to character, and compare it against valid cover angles. 
            //This allows for 3 levels of elevation (below, equal and above), and 4 angles of azimuth (fore, back, left, right). Combining these results in 12 angles of visibility. 
            //Again, not optimal in real-time (there I would increase angles of azimuth to 8, if not 16). However, for a grid based game, perfect. 
            //My current work is not perfect, as it has a far too high reliance on these damn enum switches. I really need to learn how to bit shift (although, due to the different angles required here, maybe i just need more sleep for an optimal solution):
            /*
             * if (cover.enabled)
            {
                Vector3 damageVector = origin.transform.position - transform.position;
                Cover.Direction damageDirection = Cover.Direction.Up;
                float closestAngle = Mathf.Infinity;
                List<Cover.Direction> allDirections = new List<Cover.Direction>()
                {
                    Cover.Direction.Up,
                    Cover.Direction.Down,
                    Cover.Direction.Left,
                    Cover.Direction.Right,
                    Cover.Direction.Forward,
                    Cover.Direction.Backward
                };
                foreach (Cover.Direction dir in allDirections)
                {
                    switch (dir)
                    {
                        case Cover.Direction.Up:
                            float upAng = Vector3.Angle(damageVector, Vector3.up);
                            if (upAng < closestAngle)
                            {
                                closestAngle = upAng;
                                damageDirection = Cover.Direction.Up;
                            }
                            break;
                        case Cover.Direction.Down:
                            float downAng = Vector3.Angle(damageVector, Vector3.down);
                            if (downAng < closestAngle)
                            {
                                closestAngle = downAng;
                                damageDirection = Cover.Direction.Down;
                            }
                            break;
                        case Cover.Direction.Left:
                            float leftAng = Vector3.Angle(damageVector, Vector3.left);
                            if (leftAng < closestAngle)
                            {
                                closestAngle = leftAng;
                                damageDirection = Cover.Direction.Left;
                            }
                            break;
                        case Cover.Direction.Right:
                            float rightAng = Vector3.Angle(damageVector, Vector3.right);
                            if (rightAng < closestAngle)
                            {
                                closestAngle = rightAng;
                                damageDirection = Cover.Direction.Right;
                            }
                            break;
                        case Cover.Direction.Forward:
                            float forAng = Vector3.Angle(damageVector, Vector3.forward);
                            if (forAng < closestAngle)
                            {
                                closestAngle = forAng;
                                damageDirection = Cover.Direction.Forward;
                            }
                            break;
                        case Cover.Direction.Backward:
                            float backAng = Vector3.Angle(damageVector, Vector3.back);
                            if (backAng < closestAngle)
                            {
                                closestAngle = backAng;
                                damageDirection = Cover.Direction.Backward;
                            }
                            break;
                    }
                }
                */
        }
        #endregion
    }

    //[System.Serializable]
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

    //[System.Serializable]
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

