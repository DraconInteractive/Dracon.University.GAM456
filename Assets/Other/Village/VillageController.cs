using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Village {
    public class VillageController : MonoBehaviour
    {
        public static VillageController controller;
        public int gridSize;
        [HideInInspector]
        public List<Node> allNodes = new List<Node>();
        public List<Building> allBuildings = new List<Building>();

        public LayerMask buildingMask, obstructionMask;

        public float edgeDistance;

        public bool showGrid;

        private void Awake()
        {
            controller = this;
        }
        // Use this for initialization
        void Start()
        {
            StartCoroutine(GenNodes());
        }

        // Update is called once per frame
        void Update()
        {

        }

        IEnumerator GenNodes ()
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Node newNode = new Node()
                    {
                        position = new Vector3(x, 0, z)
                    };
                    allNodes.Add(newNode);
                }
            }
            StartCoroutine(GetBuildings());
            yield break;
        }

        IEnumerator GetBuildings ()
        {
            foreach (Node n in allNodes)
            {
                Collider[] cols = Physics.OverlapSphere(n.position, 0.45f, buildingMask);
                foreach (Collider col in cols)
                {
                    Building obj = col.GetComponent<BuildingOBJ>().building;
                    n.building = obj;
                    if (!allBuildings.Contains (obj))
                    {
                        allBuildings.Add(obj);
                    }
                }
            }
            StartCoroutine(GetObstructions());
            yield break;
        }

        IEnumerator GetObstructions ()
        {
            foreach (Node n in allNodes)
            {
                Collider[] cols = Physics.OverlapSphere(n.position, 0.45f, obstructionMask);
                foreach (Collider col in cols)
                {
                    n.obstructed = true;
                }
            }
            StartCoroutine(GenerateEdges());
            yield break;
        }

        IEnumerator GenerateEdges ()
        {
            foreach (Node n in allNodes)
            {
                foreach (Node nn in allNodes)
                {
                    if (Vector3.Distance(n.position, nn.position) < edgeDistance && n != nn)
                    {
                        n.edges.Add(new Edge()
                        {
                            endNode = nn
                        });
                    }
                }
            }

            yield break;
        }

        public List<Node> GeneratePath(Node start, Node end)
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
                    if (path != null)
                    {
                        List<Node> smoothPath = SmoothPath(path);
                    }
                    
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
                    //add conditions here to stop propogation
                    /*
                    if (neighbour != end )
                    {
                        continue;
                    }
                    */
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

        public List<Node> SmoothPath (List<Node> original)
        {
            List<Node> smoothPath = new List<Node>(original);
            print("Smooth begun: " + smoothPath.Count);
            Node checkPoint = smoothPath[0];
            Node currentPoint = smoothPath[1];
            while (currentPoint != null)
            {
                Ray ray = new Ray(checkPoint.position, (currentPoint.position - checkPoint.position).normalized);
                if (Physics.SphereCast(ray, 1, Vector3.Distance(checkPoint.position, currentPoint.position), obstructionMask))
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

            return (path);
        }

        private void OnDrawGizmos()
        {
            if (!showGrid)
            {
                return;
            }
            if (allNodes.Count > 0)
            {
                foreach (Node n in allNodes)
                {
                    if (n.building != null && n.building.present)
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = Color.black;
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
                            Gizmos.color = Color.black;
                        }
                        Gizmos.DrawLine(n.position, edge.endNode.position);
                    }
                }

                foreach (Building b in allBuildings)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(b.position + Vector3.up * 2.5f, new Vector3(0.5f, 5, 0.5f));
                }
            }
        }

        public Node GetNodeFromWorldPos (Vector3 pos)
        {
            if (allNodes.Count <= 0)
            {
                print("No nodes to get: GNFWP");
                return null;
            }

            float bigDist = Mathf.Infinity;
            Node target = null;
            foreach (Node n in allNodes)
            {
                float dist = Vector3.Distance(n.position, pos);
                if (dist < bigDist)
                {
                    bigDist = dist;
                    target = n;
                }
            }

            return target;
        }

        public Building GetBuildingByType (Building.Type type)
        {
            if (allNodes.Count <= 0)
            {
                return null;
            }
            foreach (Node n in allNodes)
            {
                if (n.building != null && n.building.buildingType == type)
                {
                    return n.building;
                }
            }
            return null;
        }

        public Building GetRandomBuilding ()
        {
            Building result = null;
            result = allBuildings[Random.Range(0, allBuildings.Count)];
            return result;
        }
    }

    [System.Serializable]
    public class Node
    {
        public Vector3 position;
        public Building building;
        public bool obstructed;
        public List<Edge> edges = new List<Edge>();

        //pathfinding
        public int gCost, hCost, fCost;
        public Node parent;
    }

    [System.Serializable]
    public class Edge
    {
        public Node endNode;
    }
    [System.Serializable]
    public class Building
    {
        public bool present;
        public Vector3 position;
        public enum Type
        {
            MainHall,
            Barracks,
            Field,
            Inn
        };

        public Type buildingType;

        public GameObject GO;
    }
}

