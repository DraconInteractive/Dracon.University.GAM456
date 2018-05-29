﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Game_Controller : MonoBehaviour {

    public static Game_Controller controller;

    public enum GenerationType
    {
        Mesh,
        HexTile
    };

    public GenerationType genType;

    public int gridXSize, gridYSize;

    [Header("Mesh Gen Options")]
    public Vector3[] vertices;
    private Mesh mesh;

    [Header("Hex Grid Options")]
    public GameObject[] hexGrid;
    public List<GameObject> landHexes = new List<GameObject>();
    public GameObject hexTilePrefab;
    public Vector3 tileOffset;
    public float hexAdjust;
    public Color bottomColor, topColor, waterColor;
    public float waterThreshold;
    public float colorMod;

    [Header("Misc Options")]
    public GameObject nodePrefab;

    public float perlinOffset, perlinScale, perlinMagnitude;

    public bool useVisualisation;
    public int visCounter, visCounterTarget;

    

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
        if (genType == GenerationType.Mesh)
        {
            StartCoroutine(GenerateMesh());
        } 
        else if (genType == GenerationType.HexTile)
        {
            StartCoroutine(GenerateHexGrid());
        }
		
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

    IEnumerator GenerateHexGrid()
    {
        if (gridXSize % 2 == 0)
        {
            gridXSize--;
        }
        else
        {
            print("GXS: " + gridXSize + "% 2 == " + gridXSize % 2);
        }
        hexGrid = new GameObject[(gridXSize + 1) * (gridYSize + 1)];
        int offsetCounter = 0;
        float hexOffset = 0;
        for (int i = 0, y = 0; y <= gridYSize; y++)
        {
            
            for (int x = 0; x <= gridXSize; x++, i++)
            {
                
                offsetCounter++;
                if (offsetCounter % 2 == 0)
                {
                    hexOffset = hexAdjust;
                } 
                else
                {
                    hexOffset = 0;
                }

                hexGrid[i] = Instantiate(hexTilePrefab, new Vector3((x * tileOffset.x), 0, (y * tileOffset.z) + hexOffset), Quaternion.identity, this.transform);
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
        StartCoroutine(ApplyPerlinNoise());
        print("hex grid done");
        yield break;
    }

    IEnumerator ApplyPerlinNoise ()
    {
        if (genType == GenerationType.Mesh)
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
        }
        else if (genType == GenerationType.HexTile)
        {
            for (int i = 0; i < hexGrid.Length; i++)
            {
                float noise = (float)NoiseS3D.Noise((hexGrid[i].transform.position.x + perlinOffset) * perlinScale, (hexGrid[i].transform.position.z + perlinOffset) * perlinScale) * perlinMagnitude;
                hexGrid[i].transform.Translate(Vector3.up * noise);

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
            StartCoroutine(ColorMeHexes());
        }
        yield break;
    }

    IEnumerator ColorMeHexes ()
    {
        List<Renderer> rr = new List<Renderer>();
        float avY = 0;
        float min = Mathf.Infinity;
        float max = -Mathf.Infinity;
        foreach (GameObject go in hexGrid)
        {
            rr.Add(go.GetComponentInChildren<Renderer>());
            avY += go.transform.position.y;
            float height = go.transform.position.y;
            if (height < min)
            {
                min = height;
            }
            if (height > max)
            {
                max = height;
            }
        }
        avY /= hexGrid.Length;

        for (int i = 0; i < rr.Count; i++)
        {
            float height = hexGrid[i].transform.position.y;
            if (height < waterThreshold)
            {
                
                rr[i].material.color = waterColor;
            }
            else
            {
                float normalised = (height / 2) / (waterThreshold / 2);
                rr[i].material.color = Color.Lerp(bottomColor, topColor,Mathf.Pow(normalised, colorMod));

                landHexes.Add(hexGrid[i]);
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
        print("min: " + min + " max: " + max + " avg: " + avY);

        StartCoroutine(GenerateNodes());
        yield break;
    }
    
    IEnumerator GenerateNodes ()
    {
        if (genType == GenerationType.Mesh)
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
        }
        else if (genType == GenerationType.HexTile)
        {
            foreach (GameObject go in landHexes)
            {
                float f = Random.Range(0, 100);
                if (f > obstructionThreshold)
                {
                    Instantiate(obstructionPrefab, go.transform.position, Quaternion.identity, this.transform);
                }
                else
                {
                    Node node = Instantiate(nodePrefab, go.transform.position + Vector3.up * 0.75f, Quaternion.identity, this.transform).GetComponent<Node>();
                    NodeController.controller.nodes.Add(node);

                    node.position = go.transform.position;
                    node.tile = null;
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
