using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour {

    public Vector3 position;
    public List<NodeEdge> edges = new List<NodeEdge>();
    public Tile tile;

    public bool edgeCalculated = false;

    public int gCost;
    public int hCost;

    public Node parent;
    public Node ()
    {
        edges = new List<NodeEdge>();
    }

    public Node (List<NodeEdge> e)
    {
        edges = new List<NodeEdge>();
        foreach (NodeEdge edge in e)
        {
            edges.Add(edge);
        }
    }

    private void Awake()
    {
        edgeCalculated = false;
    }
    private void Start()
    {
        if (tile == null)
        {
            return;
        }
        if (tile.Type == Tile.TileType.Obstructed)
        {
            NodeController.controller.nodes.Remove(this);
            Destroy(this.gameObject);
        }
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}

