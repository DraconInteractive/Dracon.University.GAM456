﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Game_Controller : MonoBehaviour {

	public int gridXSize, gridYSize;
    public Vector3[] vertices;
    private Mesh mesh;
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
            }
        }
        yield return null;
        mesh.RecalculateNormals();

        StartCoroutine(ApplyPerlinNoise());
        yield break;
	}

    IEnumerator ApplyPerlinNoise ()
    {
        int counter = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            float noise = (float)NoiseS3D.Noise(vertices[i].x, vertices[i].z);
            vertices[i].y += noise;
            mesh.vertices = vertices;

            counter++;

            if (counter >= 20)
            {
                counter = 0;
                yield return null;
            }
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
