using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeController : MonoBehaviour {

    public static NodeController controller;

    public List<Node> nodes = new List<Node>();
    public List<NodeEdge> edges = new List<NodeEdge>();
    public float edgeRange;

    public GameObject edgePrefab;
    void Awake ()
    {
        controller = this;
    }

	// Use this for initialization
	void Start () {
        StartCoroutine(CreateEdges());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    IEnumerator CreateEdges ()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (Node node in nodes)
        {
            foreach (Node n in nodes)
            {
                if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange)
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
            yield return null;
        }

        StartCoroutine(SmoothEdges());

        yield break;
    }

    IEnumerator SmoothEdges ()
    {
        foreach (Node node in nodes)
        {
            foreach (Node n in nodes)
            {
                if (Vector3.Distance(node.transform.position, n.transform.position) < edgeRange)
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
            yield return null;
        }
        yield break;
    }
}
