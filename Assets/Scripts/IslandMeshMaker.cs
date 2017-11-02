using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandMeshMaker : MonoBehaviour {

	//Controls the relative height of the mountain and subsequent terrain
	//The higher the number, the steeper the mountain
	[Header("HF should be 25-30% of Size")]
	public float heightFactor = 1f;

	[Range(4, 128)]
	public int size = 128;

	public FractalTerrain terrain;

	// Use this for initialization
	void Awake () {

		Mesh island = new Mesh();

		Vector3[] vertices = new Vector3[size * size];
		Vector2[] uvs = new Vector2[vertices.Length];
		int[] triangles = new int[size * size * 6];

		Array.Clear(uvs, 0, uvs.Length);
		Array.Clear(triangles, 0, triangles.Length);

		//Find the center of the submesh
		float halfSubMeshSize = size / 2;

		//For each point, set the value of the height to a number between 0.1 and 1
		//based on how close the vertex is to the center. 
		for (int row = 0; row < size; row++) {
			for (int col = 0; col < size; col++) {
				float distanceFromCenterVert = Mathf.Lerp(1f, -0.1f, Mathf.Abs(row - halfSubMeshSize) / halfSubMeshSize);
				float distanceFromCenterHori = Mathf.Lerp(1f, -0.1f, Mathf.Abs(col - halfSubMeshSize) / halfSubMeshSize);
				float distanceFromCenter = distanceFromCenterVert * distanceFromCenterHori;
				distanceFromCenter = Mathf.Pow(distanceFromCenter, 3);

				//If the vertex is very close to the center, lower its value to create a crater
				if (distanceFromCenter > 0.8f) {
					vertices[(row * size) + col].y = ((distanceFromCenter * heightFactor) + UnityEngine.Random.Range(-0.1f, 0.1f)) * 0.75f;
				}
				else if (distanceFromCenter < 0.01f) {
					vertices[(row * size) + col].y = Mathf.Abs(((distanceFromCenter * heightFactor) + UnityEngine.Random.Range(-0.1f, 0.1f))) * -1.5f;
				}
				else {
					vertices[(row * size) + col].y = (distanceFromCenter * heightFactor) + UnityEngine.Random.Range(-0.1f, 0.1f);
				}
			}
		}

		island.vertices = vertices;
		island.uv = uvs;
		island.triangles = triangles;

		terrain.startingMesh = island;
		terrain.BuildMesh();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
