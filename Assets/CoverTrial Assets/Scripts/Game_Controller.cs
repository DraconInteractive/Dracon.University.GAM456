using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cover
{
    //main controller for the game. Contains all important functions and variables. 
    public class Game_Controller : MonoBehaviour
    {
        //singleton reference
        public static Game_Controller controller;
        //global storage of all generated game statistics. 
        public List<Tile> allTiles = new List<Tile>();
        public List<Node> allNodes = new List<Node>();
        public List<Edge> allEdges = new List<Edge>();
        public List<Character> allEnemies = new List<Character>();
        public List<Character> allAllies = new List<Character>();
        //parent object of the game tiles
        public GameObject tileContainer;
        //How far a tile can be from another tile (at maximum) to be considered a neighbour.
        //Neighbours get edges
        public float edgeDistance;
        //Visualisation is used to stagger generation of pathing for slower systems and debugging. 
        public bool useVisualisation;
        public int visCount, visCountTarget;
        //gameobject used to tell player what they have selected, and the gameobject parent of all characters
        public GameObject selectionCylinder, characterContainer;
        //The currently selected tile in game
        public Tile selectedTile;
        //Whether the nav generation should follow its course by itself (aka, FEED through) or wait for manual input.
        public bool feed;
        //is it the players turn / can they act?
        public bool playerTurn = true;
        //Masks used for various raycasts
        public LayerMask obstructionMask, interactionMask, wallMask, smoothMask;
        //show gizmos for pathing
        public bool showGrid;
        //should we smooth the paths, or let them stay as is generated
        public bool smoothPath = false;

        List<Node> highlightedPath = new List<Node>();
        LineRenderer pathRenderer;
        public List<Node> HighlightedPath
        {
            get
            {
                return highlightedPath;
            }

            set
            {
                highlightedPath = value;

                if (pathRenderer == null)
                {
                    pathRenderer = GetComponent<LineRenderer>();

                    if (pathRenderer == null)
                    {
                        print("Failure to retrieve path renderer");
                        return;
                    }
                }

                if (highlightedPath == null)
                {
                    pathRenderer.positionCount = 0;
                }
                else if (highlightedPath.Count < 2)
                {
                    pathRenderer.positionCount = 0;
                }
                else
                {
                    pathRenderer.positionCount = highlightedPath.Count;

                    for (int i = 0; i < highlightedPath.Count; i++)
                    {
                        pathRenderer.SetPosition(i, highlightedPath[i].position);
                    }
                }

                /*
                foreach (Tile t in allTiles)
                {
                    t.GetComponent<Renderer>().material.SetFloat("_Highlighted", 0);
                }
                if (value != null)
                {
                    foreach (Node n in highlightedPath)
                    {
                        GetTileFromNode(n).GetComponent<Renderer>().material.SetFloat("_Highlighted", 1);
                    }
                }
                */
            }
        }

        private void Start()
        {
            //set the singleton, and retrieve preexisting tiles
            controller = this;
            if (feed)
            {
                GetTiles();
            }
        }

        private void Update()
        {
            //Every tick, do a raycast to mouse pos. If it hits an interactable, set the selection cylinder position. 
            //If the player clicks, check what we are on. If its a tile, select it. If its a character, select the characters tile. 
            //If its a right click, and on a tile, and the player can interact, move the character to the selected tile. 
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
            //if the player presses C, and there is a selected tile, toggle crouch
            if (Input.GetKeyDown(KeyCode.C) && selectedTile != null)
            {
                ToggleCrouch();
            }
        }

        #region Generation
        //editor tool, wipe all generated items. Useful for offline generation wiping, not really needed for ingame. 
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
        //Editor and game tool. Get all existing tiles. 
        [ContextMenu("Get Tiles")]
        public void GetTiles()
        {
            //Search all children of the tile container gameobject for a tile. If the tile is not already in the list, add it. 

            Tile[] tiles = tileContainer.GetComponentsInChildren<Tile>();
            foreach (Tile t in tiles)
            {
                if (!allTiles.Contains(t))
                {
                    allTiles.Add(t);
                }
            }
            //Once finished, move on to generating nodes. 
            if (feed)
            {
                StartNodeGeneration();
            }
        }
        //Used to generate nodes from tiles
        [ContextMenu("Generate Nodes")]
        public void StartNodeGeneration()
        {
            //Clear the existing information
            //Start the coroutine with the proper instructions. 
            allNodes.Clear();
            StartCoroutine(GenerateNodes());
        }
        //^
        IEnumerator GenerateNodes()
        {
            //print("Generating Nodes");
            //Foreach tile, make a node with the position, and tile cover information. If the tile contains an obstruction, then register this also with the node. 
            foreach (Tile t in allTiles)
            {
                Node newNode = new Node()
                {
                    position = t.transform.position + t.transform.up,
                    nodeCover = t.tileCover,
                    obstructed = t.Obstructed
                };
                //Add the node to the tile, and to the global storage unit. 
                t.node = newNode;
                allNodes.Add(newNode);


                //Stagger creation for slow systems / visualisation. 
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
            //Once finished, begin generating edges. 
            if (feed)
            {
                StartEdgeGeneration();
            }
            yield break;
        }
        //Self explanatory?
        [ContextMenu("Generate Edges")]
        public void StartEdgeGeneration()
        {
            StartCoroutine(GenerateEdges());
        }
        //Begin generating edges from nodes within a distance. Somewhat unoptimised, trying to find a better way to do this. 
        IEnumerator GenerateEdges()
        {
            //for each one of our nodes in the global container, clear its edge container. 
            //Check each node in the global container for distance from the focus node. If it is less than edgeDistance, create a new edge with the end node, and a new Door stat. 
            //Add the new edge to the global contaienr and to the node.  (technically global container is not needed, but im seeing what possibilities it has for now. 
            //print("Generating Edges");
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
            //foreach node in the global container, get the edges that are obstructed, or have a door. If they do, set them to obstructed or add door statistics. 
            //Also do a check for walls. Remove all edges that pass through a wall. 
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
            //Once done, start spawning characters
            if (feed)
            {
                StartSpawnCharacters();
            }
            yield break;
        }
        //What i just said ^
        [ContextMenu("Spawn Characters")]
        public void StartSpawnCharacters()
        {
            StartCoroutine(SpawnCharacters());
        }
        //Get all the various spawn points and spawn the appropriate characters
        IEnumerator SpawnCharacters()
        {
            //print("Starting Character Spawn");
            //Foreach tile in the global container, check to see if it is a spawnpoint. 
            //If it is, spawn the character, and set their tile to the focus. 
            //Add the new character to the appropriate faction container depending on its Faction variable. 
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
        //Remnant of an inadmissable function kept only for reference. Hence, it be commented. 
        /*
        public List<Node> GeneratePath_OLD(Node start, Node end)
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
                    //Once end node is found, retrace up the parent nodes until the path forms.
                    List<Node> path = RetracePath(start, end);
                    //if the path is long enough and I say so, smooth it, then return it. 
                    //Disabling this for highlighting, and because its not worth the expense
                   
                    return path;
                }

                List<Node> neighbours = new List<Node>();
                foreach (Edge edge in currentNode.edges)
                {
                    //dont just check for obstruction, but also for doors. Reject path if door is locked.
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
                    //to stop from walking on characters, remove occupied nodes. 
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
        */
        public List<Node> GeneratePath (Node start, Node end) {
            //Righto, so adapting my own version (even more so than before i suppose?)

            //lists for all, all under consideration, and all already considered
            List<Node> nodes = new List<Node>(allNodes);
            List<Node> open = new List<Node>();
            List<Node> closed = new List<Node>();

            //What are we currently looking at? Will change depending on heuristic calculation
            Node current = start;
            //final path
            List<Node> path = new List<Node>();
            //make sure open has at least one. 
            open.Add(start);

            //keep going until we find the path, or until we run out of possible nodes. 
            while (open.Count > 0) {
                //A number to measure distance against in a second. 
                float f = Mathf.Infinity;

                //Calculate distance plus heuristic.
                //The first is the distance to the final target, the second the distance from the previous node. 
                foreach (Node n in open) {
                    n.gCost = GetDistance(n, end);
                    n.hCost = GetDistance(n, current);
                }
                //store our resultant
                Node leastF = null;
                //find the node with the lowest fcost and set it to the currently considered node. 
                foreach (Node n in open) {
                    if (n.fCost < f) {
                        f = n.fCost;
                        leastF = n;
                    }
                }
                current = leastF;
                //if the currently considered node is the target, get the path and return it. 
                if (current == end) {
                    path = RetracePath(start, end);
                    return path;
                }
                //if the node is viable, add it to open, and set its parent. 
                foreach (Edge e in current.edges) {
                    if (!e.door.locked && !e.endNode.obstructed && !open.Contains(e.endNode) && !closed.Contains(e.endNode)) {
                        open.Add(e.endNode);
                        e.endNode.parent = current;
                    }
                }
                //now that we have considered the node, remove it from consideration, and add it to the considered pile. 
                open.Remove(current);
                closed.Add(current);

            }
            return null;
        }
        //Smooth the generated path. Look for sections without turning or obstructions, and remove unnessecary nodes
        public List<Node> SmoothPath(List<Node> original)
        {
            //Get a local copy of the path. 
            List<Node> smoothPath = new List<Node>(original);
            //print("Smooth begun: " + smoothPath.Count);
            //setup the original check points
            Node checkPoint = smoothPath[0];
            Node currentPoint = smoothPath[1];
            //as long as there is a point to operate from:
            while (currentPoint != null)
            {
                //Do a raycast to see if there is a turn or an obstruction
                Ray ray = new Ray(checkPoint.position, (currentPoint.position - checkPoint.position).normalized);
                RaycastHit hit;
                //Spherecast, so its better at obstruction seeing ("thicc_cast" tm). 
                if (Physics.SphereCast(ray, 1f, out hit, Vector3.Distance(checkPoint.position, currentPoint.position), smoothMask))
                {
                    //if the path cannot continue, set the check point to the current point 
                    //This allows us to start a new set of checks. 
                    checkPoint = currentPoint;
                    //Check to see if there are sufficient nodes left in the path. If so, set the currentpoint to the next node in the path. 
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
                    //If the path continues, store the current point. 
                    //Check to see if there are enough nodes, and if so set currentpoint to next node in path
                    //Then, remove this node from minipath
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
            //return smoothed path
            print("Smooth Finished: " + smoothPath.Count);
            return smoothPath;
        }
        //used to get distance in a special, griddy way that I dont totally understand
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
        //Used to reconstruct the path from parent nodes. 
        List<Node> RetracePath(Node startNode, Node endNode)
        {
            //create a new container for the path, and a focus node starting at the end. 
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
            //While there are nodes to consider, add the node to the path, then go to the parent of that node, path wise
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            //This gives us the path backwards, so we just reverse it and return it
            path.Reverse();

            return (path);
        }

        #endregion

        #region Mouse Inputs
        //Mouse over tile, moves the selection cylinder to the tile. Deprecated, i think?
        public void MOTile(Tile t)
        {
            selectionCylinder.transform.position = t.transform.position;
        }
        //Mouse down on tile
        public void MDTile(Tile t)
        {
            HighlightedPath = null;
            //Unselect the previous tile(s)
            foreach (Tile tile in allTiles)
            {
                tile.UnSelectTile();
            }
            //set the shader float "_Selected" to 1, or true. Will make tile go blue. 
            t.GetComponent<Renderer>().material.SetFloat("_Selected", 1);
            //record the selected tile. 
            selectedTile = t;
        }
        //Mouse down (right) on tile
        public void MDRTile(Tile t)
        {
            //If its not the players turn, go away
            if (!playerTurn)
            {
                return;
            }
            List<Node> path = GeneratePath(selectedTile.node, t.node);
            if (highlightedPath != null && highlightedPath[1] == path[0] && highlightedPath[HighlightedPath.Count-1] == path [path.Count-1])
            {
                DoPlayerAction(t, path);
            }
            else
            {
                List<Node> hPath = new List<Node>(path);
                hPath.Insert(0, selectedTile.node);
                HighlightedPath = new List<Node>(hPath);
            }
        }

        void DoPlayerAction (Tile t, List<Node> path) {
            print("doing action");
            if (selectedTile != null && selectedTile.node.occupant != null)
            {
                //check to see if the click on tile is occupied, and make sure it is not the same as selected
                if (t.node.occupant != null && t != selectedTile)
                {
                    //Generate a path from the clicked to the selected. 
                    //If there is a path...
                    //...and it is less than the attack dist, damage the target on the node. 
                    //...and it is farther than the attack dist, move toward the target. 
                    //Then, end player turn, and start enemy turn
                    float enemyWait = 0;
                    //List<Node> path = GeneratePath(selectedTile.node, t.node);
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
                    //If the selected tile has no character, generate a path. 
                    float enemyWait = 0;
                    //List<Node> path = GeneratePath(selectedTile.node, t.node);
                    // if the path is valid, get the character and create a local reference. 
                    if (path != null)
                    {
                        Character ch = selectedTile.node.occupant;
                        //If the character is standing, and the path is further than the walk dist,
                        //shorten the path to a walkable dist. Then, start enemy turn, waiting for the character to stop walking. 
                        //If the path is reachable, just move to it. 
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
                        //If the character is crouching, modify the above to use crouch movepoints, not regular ones
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
                        //set this tile to be non-occupied 
                        selectedTile.node.occupant = null;
                        //start enemy turn
                        StartCoroutine(EnemyTurn(enemyWait));
                    }
                }

            }
        }
        //Set crouching state to opposite of current. 
        public void ToggleCrouch ()
        {
            //go away if its not the players turn
            if (!playerTurn)
            {
                return;
            }
            //if the selected tile is not null, and there is an occuplant, reverse their crouching state. 
            if (selectedTile != null && selectedTile.node.occupant != null)
            {
                selectedTile.node.occupant.Crouching = !selectedTile.node.occupant.Crouching;
            }
        }
        //Mouse down on character. Do same as tile, this is only here to make selection feel smoother. 
        public void MDCharacter (Character character)
        {
            MDTile(character.currentTile);
        }
#endregion
        //Helper used to get tile from node. 
        public Tile GetTileFromNode (Node node)
        {
            //Compare distances from node to tiles. Shortest distance gets returned. 
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
        //helper for getting node from world position. 
        public Node GetNodeFromWorldPos (Vector3 pos)
        {
            //Compare pos to nodes, shortest distance tofrom wins. 
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
        //Set a nodes occupant
        public void SetNodeOccupant (Node newNode, Character character)
        {
            //Find the character in the nodes, and set its node to not occupied
            //Will find a more optimised version of this soon :P
            foreach (Node n in allNodes)
            {
                if (n.occupant == character)
                {
                    n.occupant = null;
                    break;
                }
            }
            //If character cant be found, go away. 
            if (character == null)
            {
                return;
            }
            //search nodes for node then set occupant. 
            //have to do this as node got copied, not referred. 
            foreach (Node n in allNodes)
            {
                if (n == newNode)
                {
                    n.occupant = character;
                }
            }
        }

        //Perform enemy turn. Cycle through all enemies performing actions then transfer control back to player. 
        IEnumerator EnemyTurn (float wait)
        {
            //make it so the player cant interact
            playerTurn = false;
            //Wait while player actions finish
            yield return new WaitForSeconds(wait);
            //foreach Enemy in the global list, perform their action then wait for them to finish. When they all have, give control back to player and end routine. 
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
        //Draw gizmos for debuggin
        private void OnDrawGizmos()
        {
            //dont draw if disabled
            if (!showGrid)
            {
                return;
            }
            //IF there is no data, dont try to draw
            if (allNodes.Count <= 0)
            {
                return;
            }
            //draw a sphere for each node in the global container
            //Color sphere depending on obstruction status
            //Draw line for edges
            //Color edges based on obstructed, or door status. 
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
    //Class for storing node data. Serialization disabled due to editor lag
    //[System.Serializable]
    public class Node
    {
        //store position, and cover, as well as obstruction. Edges are their own class and stored in a resizable data container. 
        //Store any characters currently at this node, as well as g,h and f cost associated with pathing. Parent reference let me retrace path after generation. 
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

        //I dont use this, just made it for the weekly assignment. As such, not going to comment it further than it already was. 
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
    //Class for storing node or tile cover. 
    //[System.Serializable]
    public class Cover
    {
        //enabled lets me do some optimsation. No cover, no need to check angles. 
        public bool enabled;
        //aforementioned angles. 
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        };
        //List of which directions this node/tile has cover from. 
        public List<Direction> coverDirections = new List<Direction>();
    }
    //Edge class used for connecting nodes. stores an end, and a possible door. 
    //[System.Serializable]
    public class Edge
    {
        public Node endNode;
        public Door door;
    }
    //Door class to extend edge functionality. 
    [System.Serializable]
    public class Door
    {
        //enabled serves same purpose as for edge
        public bool enabled;
        //open tracks status, as does locked. 
        public bool open;
        public bool locked;
        //prefab is a ref to the door in the scene. useful for anim and coloring based on status. Which is what the colors are for. 
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
                //color door based on status
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
        //^
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

