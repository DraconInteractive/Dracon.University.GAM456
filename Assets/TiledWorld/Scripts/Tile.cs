using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {
	public enum TileType
	{
		Free,
		Obstructed
	}

	public TileType Type;

    public enum Aspect
    {
        City,
        Tree
    }

    public List<Aspect> details = new List<Aspect>();
    public GameObject nodePrefab;
    public Node node;

    private void Start()
    {
        //node = Instantiate(nodePrefab, transform.position + Vector3.up * 0.75f, Quaternion.identity, this.transform).GetComponent<Node>();
        //NodeController.controller.nodes.Add(node);

        //node.position = transform.position;
        //node.tile = this;

    }
}
