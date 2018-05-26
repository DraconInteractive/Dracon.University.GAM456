using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    public static MovementController controller;
    NodeController nController;

    public GameObject playerPrefab;
    GameObject player;
    private void Awake()
    {
        controller = this;
    }
    // Use this for initialization
    void Start () {
        nController = NodeController.controller;
        StartCoroutine(DoTheThing());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public IEnumerator DoTheThing ()
    {
        yield return new WaitForSeconds(1);
        player = Instantiate(playerPrefab, this.transform);

        player.transform.position = nController.nodes[0].position + Vector3.up * 0.5f;
        yield break;
    }

    public IEnumerator GeneratePath (Node start, Node end)
    {
        yield break;
    }
}
