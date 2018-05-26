using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeController : MonoBehaviour {

    public static NodeController controller;

    public List<Node> nodes = new List<Node>();
    public List<NodeEdge> edges = new List<NodeEdge>();
    public float edgeRange;

    public GameObject edgePrefab;

    public List<Node> path;

    public enum GenerationType
    {
        Linear,
        Vine
    };

    public GenerationType type;

    public bool useVisualisation;
    void Awake ()
    {
        controller = this;
    }

	// Use this for initialization
	void Start () {
        if (type == GenerationType.Linear)
        {
            StartCoroutine(CreateEdges());
        }
        else if (type == GenerationType.Vine)
        {
            StartCoroutine(CreateEdgesVineLike());
        }
        
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnDrawGizmos()
    {
        if (path != null)
        {
            foreach (Node n in path)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(n.position + Vector3.up * 0.75f, 0.1f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(n.position + Vector3.up * 0.75f, n.parent.position + Vector3.up * 0.75f);
            }
        }
        
    }
    //Note, I will use yield return null for both of these as it helps me visualise it. Probs just faster to remove it, but for now im keeping :) might put in a bool toggle for it though. 
    IEnumerator CreateEdges ()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (Node node in nodes)
        {
            foreach (Node n in nodes)
            {
                if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange && n != node)
                {
                    NodeEdge e = Instantiate(edgePrefab, (node.transform.position + n.transform.position) / 2, Quaternion.identity, this.transform).GetComponent<NodeEdge>();
                    LineRenderer l = e.gameObject.GetComponent<LineRenderer>();
                    l.positionCount = 2;
                    l.SetPosition(0, node.transform.position);
                    l.SetPosition(1, n.transform.position);

                    edges.Add(e);
                    node.edges.Add(e);
                }
            }
            if (useVisualisation)
            {
                yield return null;
            }
            
        }

        yield break;
    }

    IEnumerator CreateEdgesVineLike()
    {
        //*EDIT Note, this crashes Unity atm. I dont think Im clearing an array properly or something
        //**EDIT figured it out. It addes already completed nodes to the currentNode array. Ill add in an array to store allNodes and then remove node once completed.
        //***EDIT nope. It got further, but still crashed. 
        //****EDIT gonna check for existing edge in the check. Will also check if checkNode is equal to new node. Will make it performance heavier, but ... wont crash unity?
        //*****EDIT ffs, got further but it still accumulates. 
        //******EDIT GOT IT. Tested and working. I was doubling up on adding to the queue. Because active nodes had overlap, queue nodes were being double added. Sorted!
        print("started vine generation");
        yield return new WaitForSeconds(0.1f);

        List<Node> allNodes = new List<Node>();
        foreach (Node nnn in nodes)
        {
            allNodes.Add(nnn);
        }
        //Create iteration containers
        List<Node> currentNodes = new List<Node>();
        List<Node> nextNodes = new List<Node>();
        //Set the initial seed Node. 
        currentNodes.Add(nodes[0]);
        print("currentNode setup complete: " + nodes[0].name);
        while (currentNodes.Count > 0)
        {
            //print("Current nodes: " + currentNodes.Count + " Next nodes: " + nextNodes.Count + " allNodes: " + allNodes.Count);
            //Overall, iterate through all active nodes to see if they can be connected to any other nodes. This presents a simpler interface than linear, but it also has some weird discrepancies. Need to work on those. 
            foreach (Node node in currentNodes)
            {
                //Pretty sure this might not be working atm, but i need to check it when im awake. 
                foreach (Node n in allNodes)
                {
                    bool edgeExists = false;
                    foreach (NodeEdge edge in n.edges)
                    {
                        foreach (NodeEdge edgey in node.edges)
                        {
                            if (edge.endNode == node || edgey.endNode == n)
                            {
                                edgeExists = true;
                                break;
                            }
                        }
                    }


                    //Check if new node is old node (I need to remember to implement this in linear too)

                    if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange && n != node && !edgeExists && !currentNodes.Contains(n) && !n.edgeCalculated)
                    {
                        NodeEdge e = Instantiate(edgePrefab, (node.transform.position + n.transform.position) / 2, Quaternion.identity, this.transform).GetComponent<NodeEdge>();
                        LineRenderer l = e.gameObject.GetComponent<LineRenderer>();
                        l.positionCount = 2;
                        l.SetPosition(0, node.transform.position);
                        l.SetPosition(1, n.transform.position);

                        edges.Add(e);
                        node.edges.Add(e);

                        if (!nextNodes.Contains(n))
                        {
                            nextNodes.Add(n);
                        }
                        
                        //print("vine node added. NextNodes: " + nextNodes.Count);
                    }
                }
            }
            if (useVisualisation)
            {
                yield return null;
            }

            //Remove considered nodes as it goes - makes it go faster as it progresses, good for big maps. 
            //Set already registered nodes to calculated. 
            foreach (Node cleared in currentNodes)
            {
                allNodes.Remove(cleared);
                cleared.edgeCalculated = true;
            }
            
            //Find any shared nodes and remove. I double up on this calculation a bit here and there, i need to sort that out. 
            List<Node> sameNodes = new List<Node>();
            foreach (Node n in nextNodes)
            {
                if (currentNodes.Contains(n))
                {
                    sameNodes.Add(n);
                }
            }
            foreach (Node n in sameNodes)
            {
                nextNodes.Remove(n);
            }
            //Clear the current nodes to prepare for next iteration. 
            currentNodes.Clear();
            //Transfer queued nodes to active list
            foreach (Node n in nextNodes)
            {
                if (!n.edgeCalculated)
                {
                    currentNodes.Add(n);
                }
            }
            //Clear the queue.
            nextNodes.Clear();

            if (useVisualisation)
            {
                yield return null;
            }
        }
        print("finished vine generation");
        yield break;
    }

    public List<Node> GetNeighbours (Node node)
    {
        List<Node> neighbours = new List<Node>();
        foreach (Node n in nodes)
        {
            if (Vector3.Distance(n.position, node.position) < edgeRange)
            {
                neighbours.Add(n);
            }
        }
        return neighbours;
    }

    public Node GetNodeFromWorldPos (Vector3 pos)
    {
        float f = Mathf.Infinity;
        Node focus = null;
        foreach (Node n in nodes)
        {
            float h = Vector3.Distance(n.position, pos);
            if (h < f)
            {
                f = h;
                focus = n;
            }
        }

        return focus;
    }
}
