using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Total notes:
//Linear node creation and edge creation is a straight forward iteration of nodes with a distance loop to check for neighbours. Store that info in an edge, delete any nodes that are obstructed. BAM. 
//Vine node/edge creation goes from a seed outward. It has some weird behaviour right now, so working on THAT. Might make a Vine 2.0 to just start from scratch with that. 
//Gonna do that ^. Calling it Ivy. 
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
        Vine,
        Ivy
    };

    public GenerationType type;

    public bool useVisualisation;
    public int visCounter, visCounterTarget;

    public LayerMask obstructionMask;
    public float obstructionDetectWidth;
    public bool vertFeed;
    void Awake ()
    {
        controller = this;
    }

	// Use this for initialization
	void Start () {
        if (!vertFeed)
        {
            DoEdges();
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

    public void DoEdges ()
    {
        if (type == GenerationType.Linear)
        {
            StartCoroutine(CreateEdges());
        }
        else if (type == GenerationType.Vine)
        {
            StartCoroutine(CreateEdgesVineLike());
        } 
        else if (type == GenerationType.Ivy)
        {
            StartCoroutine(CreateEdgesIvy());
        }
    }
    //Note, I will use yield return null for both of these as it helps me visualise it. Probs just faster to remove it, but for now im keeping :) might put in a bool toggle for it though. 
    //Yup, put in that toggle. If i just let it go it crashes on my old laptop due to my old laptop being shit.
    IEnumerator CreateEdges ()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (Node node in nodes)
        {
            foreach (Node n in nodes)
            {
                if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange && n != node)
                {
                    
                    Ray ray = new Ray(node.position, n.position - node.position);
                    RaycastHit hit;
                    if (!Physics.SphereCast(ray, obstructionDetectWidth, out hit, 2, obstructionMask))
                    {
                        NodeEdge e = Instantiate(edgePrefab, (node.transform.position + n.transform.position) / 2, Quaternion.identity, this.transform).GetComponent<NodeEdge>();
                        LineRenderer l = e.gameObject.GetComponent<LineRenderer>();
                        l.positionCount = 2;
                        l.SetPosition(0, node.transform.position);
                        l.SetPosition(1, n.transform.position);

                        e.endNode = n;
                        edges.Add(e);
                        node.edges.Add(e);
                    }                    
                }
            }
            if (useVisualisation)
            {
                if (visCounter >= visCounterTarget)
                {
                    visCounter = 0;
                    yield return null;
                }
                else
                {
                    visCounter++;
                }
            }
            
        }
        if (MovementController.controller != null)
        {
            StartCoroutine(MovementController.controller.DoTheThing());
        }
        
        print("linear done");
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
            //Soooo... looks like the discrepancies are here to stay? Cant seem to shake them.
            //In the mesh gen version, this doesnt matter so much. Player needs to go left? Do a little loop. Its almost more realistic. Its the tile version that concerns me here. 
            foreach (Node node in currentNodes)
            {
                //Pretty sure this might not be working atm, but i need to check it when im awake. *EDIT I think Ive actually forgotton to SET the node edges... oops, didnt need them for my other algorithm till now.
                //Node edges set for this and linear. 
                foreach (Node n in allNodes)
                { 
                    //Check if new node is old node (I need to remember to implement this in linear too) //Implemented in linear. 

                    if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange && n != node && !currentNodes.Contains(n) && !n.edgeCalculated)
                    {
                        NodeEdge e = Instantiate(edgePrefab, (node.transform.position + n.transform.position) / 2, Quaternion.identity, this.transform).GetComponent<NodeEdge>();
                        e.endNode = n;
                        LineRenderer l = e.gameObject.GetComponent<LineRenderer>();
                        l.positionCount = 2;
                        l.SetPosition(0, node.transform.position);
                        l.SetPosition(1, n.transform.position);

                        e.endNode = n;
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
            if (useVisualisation && visCounter >= visCounterTarget)
            {
                if (visCounter >= visCounterTarget)
                {
                    yield return null;
                }
                else
                {
                    visCounter++;
                }
                
            }

            //Remove considered nodes as it goes - makes it go faster as it progresses, good for big maps. 
            //Set already registered nodes to calculated. 
            //I think this is what is causing my irregularity in edge assignation. Gonna do some tests. 
            //Well i was right about it speeding things up. Just almost crashed Unity. Good thing I had Vis enabled. 
            //Righto, will check some other things and come back to this then. 
            foreach (Node cleared in currentNodes)
            {
                allNodes.Remove(cleared);
                cleared.edgeCalculated = true;
            }
            
            //Find any shared nodes and remove. I double up on this calculation a bit here and there, i need to sort that out. 
            //Yehhh so disabling this made things go about 10% faster, so ima keep it gone. 
            /*
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
            */
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
        MovementController.controller.StartCoroutine(MovementController.controller.DoTheThing());
        yield break;
    }

    //Modelling this off the A* pseudocode I was studying for the pathfinding. It SHOULD work. It probably wont, but it should...
    IEnumerator CreateEdgesIvy ()
    {
        //In case you're wondering why this is here, its so if things go wrong in the processing queue, theres a buffer of a few frames for things to sort themselves out. Technically redundant, but its my airbag. 
        yield return new WaitForSeconds(0.1f);

        List<Node> activeNodes = new List<Node>();
        List<Node> finishedNodes = new List<Node>();

        activeNodes.Add(nodes[0]);
        while (activeNodes.Count > 0)
        {
            Node current = activeNodes[0];

            foreach (Node n in nodes)
            {
                float dist = Vector3.Distance(current.position, n.position);

                if (dist < edgeRange && current != n)
                {
                    Ray ray = new Ray(current.position, n.position - current.position);
                    RaycastHit hit;
                    if (!Physics.SphereCast(ray, obstructionDetectWidth, out hit, 2, obstructionMask))
                    {
                        NodeEdge e = Instantiate(edgePrefab, (current.transform.position + n.transform.position) / 2, Quaternion.identity, this.transform).GetComponent<NodeEdge>();
                        LineRenderer l = e.gameObject.GetComponent<LineRenderer>();
                        l.positionCount = 2;
                        l.SetPosition(0, current.transform.position);
                        l.SetPosition(1, n.transform.position);

                        e.endNode = n;
                        edges.Add(e);
                        current.edges.Add(e);

                        activeNodes.Add(n);
                    }
                }
            }


            activeNodes.Remove(current);
            finishedNodes.Add(current);

            if (useVisualisation)
            {
                if (visCounter >= visCounterTarget)
                {
                    visCounter = 0;
                    yield return null;
                }
                else
                {
                    visCounter++;
                }
            }
        }

        if (MovementController.controller != null)
        {
            StartCoroutine(MovementController.controller.DoTheThing());
        }

        print("Finished ivy generation");
        yield break;
    }
    public List<Node> GetNeighbours (Node node)
    {
        List<Node> neighbours = new List<Node>();

        foreach (NodeEdge edge in node.edges)
        {
            neighbours.Add(edge.endNode);
        }

        //FEAR THE DEPRECATION. LOVE THE DEPRECATION. BE THE DEPRECATION
        /*
        if (controller.type == GenerationType.Linear)
        {
            foreach (NodeEdge edge in node.edges)
            {
                neighbours.Add(edge.endNode);
            }
        }
        else
        {
            //Making vine use distance check as it still doesnt have good edge making in yet. Once i can get it reliably tagging all nearby nodes I will just deprecate this if. 
            foreach (Node n in nodes)
            {
                if (Vector3.Distance(n.position, node.position) < edgeRange)
                {
                    RaycastHit hit;
                    if (!Physics.SphereCast(new Ray(node.position, n.position - node.position), obstructionDetectWidth, out hit, 1, obstructionMask))
                    {
                        neighbours.Add(n);
                    }
                }
            }
        }
        */
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
