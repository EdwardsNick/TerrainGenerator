using System;
using UnityEngine;


/*
	Author: Nicholas Edwards #3030212

	Created: April 10, 2017

	Description: This class creates a moving plane that simulates
	water waves
*/
[RequireComponent(typeof(MeshFilter))]
public class WaveGenerator : MonoBehaviour {

	//The arrays to be used for our mesh object
	Vector3[] vertices;
	Vector2[] uvs;
	int[] triangles;

	//Our mesh and mesh filter objects
	MeshFilter meshFilter;
	Mesh mesh;

	//The distance between vertices, increase to stretch
	public int vertexDistance = 1;

	//The size of the mesh on a side. Limited by Unity
	[Range(3, 253)]
	public int size = 253;

	//Holds the location of the middle of the mesh
	int middle;

	//The square root of 2, used to weight the diagonals during the averaging
	float diagonalWeight = 0.70710678118654752440084436210485f;

	//The speed at which the wave propagates
	[Header("Wave Travel Speed")]
	public float PROPAGATION = 1;

	//The speed at which the oscillators oscillate
	[Header("Vertical Wave Speed")]
	public float DRIVER = 1;

	//The height the oscillators go to
	[Header("Wave Height")]
	public float AMPLITUDE = 1;

	//The array of new heights to be set
	float[] newHeights;

	//The array of velocities
	float[] velocities;

	// Use this for initialization
	void Start() {
		//Ensure the mesh size is odd
		if (size % 2 == 0) {
			if (size - 1 < 0) {
				size--;
			}
			else {
				throw new Exception("Rubber Sheet size is too small!");
			}
		}
		middle = (size - 1) / 2; //find the middle

		//Set up the mesh arrays
		vertices = new Vector3[size * size];
		uvs = new Vector2[size * size];
		triangles = new int[uvs.Length * 6];

		//Get the Mesh filter and make the Mesh object
		meshFilter = GetComponent<MeshFilter>();
		mesh = new Mesh();

		//Create our height array and clear it
		newHeights = new float[size * size];
		Array.Clear(newHeights, 0, newHeights.Length);

		//create our velocities array and clear ir
		velocities = new float[size * size];
		Array.Clear(velocities, 0, velocities.Length);

		//Sets the verticies and UVS to a base value
		initializeVertAndUVs();

		//Sets up our triangles
		initializeTris();

		//Assigns the arrays to the mesh
		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.RecalculateNormals();
		mesh.triangles = triangles;

		//Assigns the mesh to the filter
		meshFilter.mesh = mesh;
	}

	// Update is called once per frame
	void Update() {

		//For each vertex except the middle oscillator
		for (int row = 0; row < size; row++) {
			for (int col = 0; col < size; col++) {
				if (row != middle && col != middle) {
					//the index of the vertex
					int index = (row * size) + col;

					//Calculate the new heights and clamp to the amplitude
					newHeights[index] = Mathf.Clamp(vertices[index].y + (velocities[index]), -AMPLITUDE, AMPLITUDE);

					//Find the average hieght of neighbors
					float averagedHeight = calculateCardinalValue(row, col, size);

					//Use that average to determine the velocity
					velocities[index] += (averagedHeight - vertices[index].y) * PROPAGATION * Time.deltaTime;
				}
			}
		}

		//Set the heights to the new heights
		for (int i = 0; i < size * size; i++) {
			vertices[i].y = newHeights[i];
		}

		//Oscillate the middle vertex
		vertices[(middle * size) + middle].y = AMPLITUDE * Mathf.Sin(Time.time * DRIVER);

		//Sets the Mesh Arrays as dirty
		ResetMesh();
	}


	/*
	Desc: Sets the vertex x,z coords and sets a basic UV value, overridden later

	parameters:

	Returns:

	Pre:
	vertices is initialized
	uvs is initialized
	size != 0
	
	*/
	void initializeVertAndUVs() {
		float UVFactor = 1.0f / size;

		for (int row = 0; row < size; row++) {
			for (int col = 0; col < size; col++) {

				//Sets our uvs and vertices based on our row and column
				vertices[(row * size) + col] = new Vector3(row * vertexDistance, 0, col * vertexDistance);
				uvs[(row * size) + col] = new Vector2(col * UVFactor, row * UVFactor);
			}
		}
	}

	/*
	Desc: Sets up the triangle indexes for Unity. 

	parameters:

	Returns:

	Pre: 
	triangles is initialized

	*/
	void initializeTris() {
		int triPosition = 0;

		//Sets all our triangles indexes
		for (int row = 0; row < size - 1; row++) {
			for (int col = 0; col < size - 1; col++) {
				int lowerLeft = (row * size) + col;
				int upperLeft = lowerLeft + size;
				triangles[triPosition++] = lowerLeft;
				triangles[triPosition++] = lowerLeft + 1;
				triangles[triPosition++] = upperLeft;
				triangles[triPosition++] = lowerLeft + 1;
				triangles[triPosition++] = upperLeft + 1;
				triangles[triPosition++] = upperLeft;
			}
		}
	}

	/*
	Desc: Find the average of the vertices around the given one 
	at a Radius of 1. 

	parameters:
	int row: The row of the vertex to find the average of 
	int col: The column of the vertex to find the average of

	Returns:
	float: The new height value to be used

	Pre: 
	-1 < row < max
	-1 < col < max
	*/
	float calculateCardinalValue(int row, int col, int max) {
		int colPlus1 = col + 1;
		int colMinus1 = col - 1;
		int rowPlus1 = row + 1;
		int rowMinus1 = row - 1;


		float nwNeighbor = 1;
		float neNeighbor = 1;
		float seNeighbor = 1;
		float swNeighbor = 1;
		float nNeighbor = 1;
		float sNeighbor = 1;
		float eNeighbor = 1;
		float wNeighbor = 1;

		if (rowPlus1 >= max) {
			neNeighbor = 0;
			nNeighbor = 0;
			nwNeighbor = 0;
		}
		else if (rowMinus1 < 0) {
			seNeighbor = 0;
			sNeighbor = 0;
			swNeighbor = 0;
		}
		if (colPlus1 >= max) {
			neNeighbor = 0;
			seNeighbor = 0;
			eNeighbor = 0;
		}
		else if (colMinus1 < 0) {
			nwNeighbor = 0;
			swNeighbor = 0;
			wNeighbor = 0;
		}

		if (neNeighbor == 1) neNeighbor = vertices[((rowPlus1) * size) + colPlus1].y * diagonalWeight;
		if (nwNeighbor == 1) nwNeighbor *= vertices[((rowPlus1) * size) + colMinus1].y * diagonalWeight;
		if (seNeighbor == 1) seNeighbor *= vertices[((rowMinus1) * size) + colPlus1].y * diagonalWeight;
		if (swNeighbor == 1) swNeighbor *= vertices[((rowMinus1) * size) + colMinus1].y * diagonalWeight;
		if (nNeighbor == 1) nNeighbor *= vertices[((rowPlus1) * size) + col].y;
		if (sNeighbor == 1) sNeighbor *= vertices[((rowMinus1) * size) + col].y;
		if (wNeighbor == 1) wNeighbor *= vertices[(row * size) + colMinus1].y;
		if (eNeighbor == 1) eNeighbor *= vertices[((row * size)) + colPlus1].y;

		return (nNeighbor + sNeighbor + eNeighbor + wNeighbor + nwNeighbor + neNeighbor + swNeighbor + seNeighbor) / 8f;
	}

	/*
	Desc: Resets the arrays to the mesh to rebuild it

	parameters:

	Returns:
	
	Post: The vertices are updated in the mesh object, with
	new normals as well
	
	*/
	void ResetMesh() {
		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		meshFilter.mesh = mesh;
	}
}
