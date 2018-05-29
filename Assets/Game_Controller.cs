using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Game_Controller : MonoBehaviour {

    public static Game_Controller controller;
	public int gridXSize, gridYSize;
    public Vector3[] vertices;
    private Mesh mesh;
    public float perlinOffset, perlinScale, perlinMagnitude;

    public bool useVisualisation;
    public int visCounter, visCounterTarget;

    public GameObject nodePrefab;

    [Range(0,100)]
    public float obstructionThreshold;

    public GameObject obstructionPrefab;

    public bool hidePathingOnFinish;
    private void Awake()
    {
        controller = this;
    }
    // Use this for initialization
    void Start () {
		StartCoroutine (GenerateMesh ());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	IEnumerator GenerateMesh () {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        vertices = new Vector3[(gridXSize + 1) * (gridYSize + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        for (int i = 0, y = 0; y <= gridYSize; y++)
        {
            for (int x = 0; x <= gridXSize; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, y);
                uv[i] = new Vector2((float)x / gridXSize, (float)y / gridYSize);
                tangents[i] = tangent;

                if (useVisualisation)
                {
                    visCounter++;
                    if (visCounter > visCounterTarget)
                    {
                        visCounter = 0;
                        yield return null;
                    }
                }
            }
        }

        yield return null;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.tangents = tangents;

        int[] triangles = new int[gridXSize * gridYSize *6];
        for (int ti = 0, vi = 0, y = 0; y < gridYSize; y++, vi++)
        {
            for (int x = 0; x < gridXSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + gridXSize + 1;
                triangles[ti + 5] = vi + gridXSize + 2;
                mesh.triangles = triangles;

                if (useVisualisation)
                {
                    visCounter++;
                    if (visCounter > visCounterTarget)
                    {
                        visCounter = 0;
                        yield return null;
                    }
                }
            }
        }
        yield return null;
        mesh.RecalculateNormals();

        StartCoroutine(ApplyPerlinNoise());
        yield break;
	}

    IEnumerator ApplyPerlinNoise ()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float noise = (float)NoiseS3D.Noise((vertices[i].x + perlinOffset) * perlinScale, (vertices[i].z + perlinOffset) * perlinScale) * perlinMagnitude;
            vertices[i].y += noise;
            mesh.vertices = vertices;

            if (useVisualisation)
            {
                visCounter++;
                if (visCounter > visCounterTarget)
                {
                    visCounter = 0;
                    yield return null;
                }
            }
        }

        StartCoroutine(GenerateNodes());
        yield break;
    }

    IEnumerator GenerateNodes ()
    {
        foreach (Vector3 vert in vertices)
        {
            float f = Random.Range(0, 100);
            if (f > obstructionThreshold)
            {
                Instantiate(obstructionPrefab, vert, Quaternion.identity, this.transform);
            }
            else
            {
                Node node = Instantiate(nodePrefab, vert + Vector3.up * 0.75f, Quaternion.identity, this.transform).GetComponent<Node>();
                NodeController.controller.nodes.Add(node);

                node.position = vert;
                node.tile = null;
            }
           
            if (useVisualisation)
            {
                visCounter++;
                if (visCounter > visCounterTarget)
                {
                    visCounter = 0;
                    yield return null;
                }
            }
        }

        NodeController.controller.DoEdges();
        yield break;
    }

    public void StartHidePathing ()
    {
        if (hidePathingOnFinish)
        {
            StartCoroutine(HidePathing());
        }
    }

   IEnumerator HidePathing ()
   {
        NodeController nController = NodeController.controller;
        foreach (Node n in nController.nodes)
        {
            n.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        yield return null;
        foreach (NodeEdge edge in nController.edges)
        {
            edge.gameObject.GetComponent<LineRenderer>().enabled = false;
        }
        yield break;
   }
    /*
    private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }
        Gizmos.color = Color.black;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], 0.1f);
        }
    }
    */
}
