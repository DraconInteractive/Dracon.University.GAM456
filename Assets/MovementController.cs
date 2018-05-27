using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    public static MovementController controller;
    NodeController nController;

    public GameObject playerPrefab;
    GameObject player, goal;

    public Transform seeker, target;

    public LineRenderer pathRenderer;
    private void Awake()
    {
        controller = this;
    }
    // Use this for initialization
    void Start () {
        nController = NodeController.controller;
        //StartCoroutine(DoTheThing());
	}
	
	// Update is called once per frame
	void Update () {
        //if (seeker != null && target != null)
            //GeneratePath(nController.GetNodeFromWorldPos(seeker.position), nController.GetNodeFromWorldPos(target.position));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GeneratePath(nController.GetNodeFromWorldPos(seeker.position), nController.GetNodeFromWorldPos(target.position));
        }
    }

    public IEnumerator DoTheThing ()
    {
        yield return new WaitForSeconds(1);
        player = Instantiate(playerPrefab, this.transform);
        goal = Instantiate(playerPrefab, this.transform);
        player.transform.position = nController.nodes[0].position + Vector3.up * 0.5f;
        goal.transform.position = nController.nodes[nController.nodes.Count - 1].position + Vector3.up * 0.5f;
        seeker = player.transform;
        target = goal.transform;

        GeneratePath(nController.GetNodeFromWorldPos(seeker.position), nController.GetNodeFromWorldPos(target.position));
        yield break;
    }

    public void GeneratePath (Node start, Node end)
    {
        List<Node> allNodes = NodeController.controller.nodes;
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
                RetracePath(start, end);
                return;
            }

            foreach (Node neighbour in nController.GetNeighbours(currentNode))
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


        return;
    }

    int GetDistance (Node nodeA, Node nodeB)
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

    void RetracePath (Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        nController.path = path;

        if (pathRenderer == null)
        {
            GameObject newObject = new GameObject("PathRenderer");
            pathRenderer = newObject.AddComponent<LineRenderer>();
            pathRenderer.startWidth = 0.1f;
            pathRenderer.endWidth = 0.1f;
        }

        pathRenderer.useWorldSpace = true;
        pathRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            pathRenderer.SetPosition(i, path[i].position + Vector3.up * 0.75f);
        }
    }

}
