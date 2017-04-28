using System;
using System.Collections.Generic;
using UnityEngine;



/*
	Author: Nicholas Edwards #3030212

	Created: April 10, 2017

	Description: This is a class for creating a fractal terrain based
	on an initial mesh provided in code, or via the Unity Editor
*/
public class FractalTerrain : MonoBehaviour {

	//////////// Structures ////////////

	//Contains all the relevent information about a submesh. Allows for easy reseting of vertices
	struct subMeshStruct {
		public Mesh subMesh;
		public MeshCollider collider;
		public int row; //the row of the submesh in the array of submeshes making up the main mesh
		public int col; // the col " " " ...

		public subMeshStruct(Mesh mesh, MeshCollider collide, int r, int c) {
			subMesh = mesh;
			collider = collide;
			row = r;
			col = c;
		}
	}


	////////// Public Variables ///////////

	//Controls the number of fractal expansions done on subMeshSize
	[Range(0, 10)]
	[Header("Quadrouples # of Vertices")]
	public int fractalExpansions = 4;

	//The distance between individual vertices in the overall mesh.
	//Increasing this will have an effect of stretching the mesh out
	[Range(1, 100)]
	[Header("Distance between Vertices")]
	public int vertexDistance = 1;

	//Controls how much of a random factor is used to determine the fractal
	//expansions
	[Range(0.0f, 1.0f)]
	[Header("Amount of Random deviation for Heights")]
	public float randomizationFactor;

	//The gameObject that will be instantiated to hold one submesh
	//Should be an empty gameObject with a Mesh and MeshFilter attached
	[Header("The Gameobject for the new Mesh")]
	public GameObject meshObject;

	//The wave generator object to spawn for the ocean
	[Header("The gameobject with Wave Gen")]
	public GameObject water;

	//Should we create the ocean
	public bool createOcean  = true;

	//Should we set uvs based on height
	public bool heightBasedUvs = true;

	//The initial mesh to build the terrain based on. It must be a square
	//mesh with a power of 2 number of vertices on a side, and it must be 128
	//vertices on a side of less
	[Header("Max size of square mesh = 128x128")]
	public Mesh startingMesh = null;



	////////// Private Variables ///////////

	//Arrays to hold the entire data set of vertices, uvs, and triangles
	Vector3[] vertices;
	Vector2[] uvs;
	int[] triangles;

	//Is the total number of vertices per side on the entire mesh
	[Header("Calculated Automatically")]
	int size;

	//The heightest Point on the Mesh
	float maxPositiveHeight = 0;

	//The lowest posint on the mesh
	float maxNegativeHeight = 0;

	//Ensures we don't accidentally build a mesh twice
	bool meshBuilt = false;

	//This is a list of subMesh structs that holds all the info we will need about our submeshes
	List<subMeshStruct> subMeshes;

	//The size of the first mesh to be fractally expanded as well as
	//size of the subMeshes used
	int subMeshSize = 128;


	//Stores the total number of sub meshes per side on the entire mesh
	int subMeshesOnASide;

	public int Size {
		get {
			return size;
		}

		set {
			size = value;
		}
	}

	public float MaxPositiveHeight {
		get {
			return maxPositiveHeight;
		}

		set {
			maxPositiveHeight = value;
		}
	}

	public float MaxNegativeHeight {
		get {
			return maxNegativeHeight;
		}

		set {
			maxNegativeHeight = value;
		}
	}


	/////////// Functions and Methods ////////////

	void Start() {
		if (startingMesh != null && !meshBuilt) {
			BuildMesh();
		}
	}



	/*
	Desc: Builds the mesh based on the starting mesh and set parameters.
		Also checks to ensure the starting mesh is correct.
	*/
	public void BuildMesh() {

		meshBuilt = true;

		//If the startingMesh is null throw a null reference exception
		if (startingMesh == null) {
			throw new NullReferenceException("StartingMesh was not defined");
		}

		//find the size of the starting mesh on a side
		int startingMeshSize = ((int)Mathf.Sqrt(startingMesh.vertices.Length));

		//find the size of our submeshse
		subMeshSize = Mathf.ClosestPowerOfTwo(startingMeshSize);

		//if the submesh size is larger than the total size of the starting mesh
		//divide by 2 to get the next smallest a power of 2
		if (subMeshSize > startingMeshSize) subMeshSize /= 2;

		//If the starting mesh was too small or too large throw an exception
		if (subMeshSize <= 0 || subMeshSize > 128) {
			throw new Exception("Starting Mesh must be a square mesh with maximum size of 128 x 128 and a minimum size of 2");
		}

		//initialize the entire mesh and all components (vertices, uvs, etc.)
		InitializeMesh();

		//Sets up the initial mesh that will the baseline for the fractalling
		InitializeStartingMesh();

		//Fractally Expands the mesh
		FractalExpand();

		//Sets our UVs to be based on height for us with our height based texture map
		if (heightBasedUvs) SetHeightBasedUVs();

		//Set the vertices back to the mesh to update them
		RebuildMesh();

		//Creates the wave generators that simulate ocean
		if (createOcean) BuildOcean();

		//moves the island to be centered at the origin
		transform.Translate(-(Size / 2) * vertexDistance, 0, -(Size / 2) * vertexDistance);
	}



	/*
	Desc: Initializes all the arrays for use by the mesh objects
	Sets up the base values for all the arrays as well

	parameters:

	Returns:

	Post:
	subMeshes is initialized
	vertices is initialized
	uvs is initialized
	triangles is initialized
	*/
	void InitializeMesh() {
		//Determines how many submeshes will be on a side 
		subMeshesOnASide = 1;
		for (int i = 0; i < fractalExpansions; i++) {
			subMeshesOnASide *= 2;
		}

		//determines the total number of vertices per side
		Size = (subMeshesOnASide * subMeshSize) + 1;

		//initializes the list of subMeshes
		subMeshes = new List<subMeshStruct>(subMeshesOnASide * subMeshesOnASide);


		//Sets the size of all the arrays we need
		vertices = new Vector3[(Size) * (Size)];
		uvs = new Vector2[vertices.Length];
		triangles = new int[(Size - 1) * (Size - 1) * 6];

		//sets the basic values for all Arrays
		InitializeVertAndUVs();
		InitializeTris();

		//Sets up all the submeshes
		InitializeSubMeshes();
	}

	/*
	Desc: Sets the vertec x,z coords and sets a basic UV value, overridden later

	parameters:


	Returns:

	Pre: 
	vertices is initialized
	uvs is initialized
	size != 0

	*/
	void InitializeVertAndUVs() {
		float UVFactor = 1.0f / Size; //The UV factor in relation to size

		for (int row = 0; row < Size; row++) {
			for (int col = 0; col < Size; col++) {
				vertices[(row * Size) + col] = new Vector3(row * vertexDistance, 0, col * vertexDistance);
				uvs[(row * Size) + col] = new Vector2(col * UVFactor, row * UVFactor);
			}
		}
	}

	/*
	Desc: Sets up the triangle indexs for Unity. 

	parameters:


	Returns:

	Pre:
	triangles is initialized

	*/
	void InitializeTris() {
		int triPosition = 0;

		for (int row = 0; row < Size - 1; row++) {
			for (int col = 0; col < Size - 1; col++) {
				int lowerLeft = (row * Size) + col;
				int upperLeft = lowerLeft + Size;
				triangles[triPosition++] = lowerLeft;
				triangles[triPosition++] = upperLeft;
				triangles[triPosition++] = lowerLeft + 1;
				triangles[triPosition++] = lowerLeft + 1;
				triangles[triPosition++] = upperLeft;
				triangles[triPosition++] = upperLeft + 1;
			}
		}
	}

	/*
	Desc: Creates and applies arrays to all the subMeshes.
	Also add as relevant data to the subMeshStructs list

	parameters:


	Returns:

	Pre: 
	this gameobject has a MeshFilter component
	this gameObject has a MeshCollider component
	subMeshes is initialized

	Post:
	MeshFilter has mesh assigned
	MeshCollider has mesh assigned

	*/
	void InitializeSubMeshes() {
		//foreach subMesh
		for (int row = 0; row < subMeshesOnASide; row++) {
			for (int col = 0; col < subMeshesOnASide; col++) {

				//Create the subMesh attached to this object
				GameObject currentObj = Instantiate(meshObject, Vector3.zero, transform.rotation, transform);

				//Get the mesh Filter
				MeshFilter filter = currentObj.GetComponent<MeshFilter>();

				//Create a Mesh
				Mesh subMesh = new Mesh();

				//Mark the mesh as dynamic to improve loading times
				subMesh.MarkDynamic();

				//Set the subMesh vertices,uvs, and triangles from the main array
				SetSubMeshArrays(subMesh, row, col);

				//Set the mesh to the filter
				filter.mesh = subMesh;

				//setup the mesh colider
				MeshCollider collider = currentObj.GetComponent<MeshCollider>();
				collider.sharedMesh = subMesh;

				//Add all relevent data to the subMeshStruct List
				subMeshes.Add(new subMeshStruct(subMesh, collider, row, col));
			}
		}
	}

	/*
	Desc: Sets the correct vertices, uvs, and triangles to the submesh

	parameters:
	Mesh mesh: The mesh to initialize
	int subMeshRow: The row of the subMesh
	int subMeshCol: The column of the subMesh


	Returns:

	Pre: 
	Mesh is initialized
	0 < subMeshRow < subMeshesOnASide
	0 < subMeshCol < subMeshesOnASide

	mesh has values assigned to arrays

	*/
	void SetSubMeshArrays(Mesh mesh, int subMeshRow, int subMeshCol) {
		//Set the size of the subMesh
		int rows = subMeshSize + 1;
		int cols = subMeshSize + 1;

		//Find the very bottom left most vertex 
		int bottomLeftSubMeshVertex = (subMeshRow * Size * subMeshSize) + (subMeshCol * subMeshSize);

		//Create new arrays
		Vector3[] vertArray = new Vector3[rows * cols];
		Vector2[] uvArray = new Vector2[rows * cols];

		//Set the vertices from the main arrays to the correct subMesh locations
		int pos = 0;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
				vertArray[pos] = vertices[bottomLeftSubMeshVertex + (i * Size) + j];
				uvArray[pos++] = uvs[bottomLeftSubMeshVertex + (i * Size) + j];

			}
		}


		//Find the correct triangles for the subMesh
		int[] trisArray = new int[subMeshSize * subMeshSize * 6];
		pos = 0;

		for (int i = 0; i < subMeshSize; i++) {
			for (int j = 0; j < subMeshSize; j++) {
				trisArray[pos++] = (i * (cols)) + j + 1;
				trisArray[pos++] = ((i + 1) * (cols)) + j;
				trisArray[pos++] = (i * (cols)) + j;

				trisArray[pos++] = ((i + 1) * (cols)) + j + 1;
				trisArray[pos++] = ((i + 1) * (cols)) + j;
				trisArray[pos++] = (i * (cols)) + j + 1;
			}
		}


		//Sets the arrays to the Mesh and lets Unity calculate normals
		mesh.vertices = vertArray;
		mesh.uv = uvArray;
		mesh.RecalculateNormals();
		mesh.triangles = trisArray;
	}

	/*
	Desc: Sets up the first mesh to do the fractal expansion on by copying
		over the height values from the starting mesh. x, z coords are 
		based on vertex distance set above not on starting mesh

	parameters:

	Returns:

	Pre: 
	vertices is initialized on startingMesh

	*/
	void InitializeStartingMesh() {
		for (int i = 0; i < subMeshSize; i++) {
			for (int j = 0; j < subMeshSize; j++) {
				vertices[(i * Size) + j].y = Mathf.Pow(2f, fractalExpansions) * startingMesh.vertices[(i * subMeshSize) + j].y;
			}
		}
	}

	/*
	Desc: Does the fractal expansion up the number of requested times

	parameters:

	Returns:

	Pre:
	vertices in initialized

	*/
	void FractalExpand() {

		for (int i = 1; i <= fractalExpansions; i++) {

			//Find the sizes of the current SubMesh and the new Mesh to expand into
			int newMeshSize = ((int)Mathf.Pow(2, i) * subMeshSize) + 1;
			int currentSubMeshSize = ((int)Mathf.Pow(2, i - 1) * subMeshSize) + 1;

			//Create an array to hold the new vertices we will generate
			Vector3[] newVertices = new Vector3[newMeshSize * newMeshSize];

			//Move the vertices from the current mesh to the new mesh at 
			//twice the index
			for (int row = 0; row < currentSubMeshSize; row++) {
				for (int col = 0; col < currentSubMeshSize; col++) {
					newVertices[((row * newMeshSize) * 2) + (col * 2)].y = vertices[(row * Size) + col].y;
				}
			}


			//clears the heights of the Vertices in the new sub mesh 
			for (int row = 0; row < newMeshSize; row++) {
				for (int col = 0; col < newMeshSize; col++) {
					vertices[(row * Size) + col].y = 0;
				}
			}

			//Moves the heights from the new sub mesh back into the main mesh
			for (int row = 0; row < newMeshSize; row++) {
				for (int col = 0; col < newMeshSize; col++) {
					vertices[(row * Size) + col].y = newVertices[(row * newMeshSize) + col].y;
				}
			}

			//Computes the y values for the new vertices on the diagonals
			for (int row = 1; row < newMeshSize; row += 2) {
				for (int col = 1; col < newMeshSize; col += 2) {
					vertices[(row * Size) + col].y = CalculateDiagonalValue(row, col);
				}
			}

			//Computes the y values for the new vertices on the cardinals
			for (int row = 0; row < newMeshSize; row++) {
				if (row % 2 == 0) {
					for (int col = 1; col < newMeshSize - 1; col += 2) {
						vertices[(row * Size) + col].y = CalculateCardinalValue(row, col, newMeshSize);
					}
				}
				else {
					for (int col = 0; col < newMeshSize; col += 2) {
						vertices[(row * Size) + col].y = CalculateCardinalValue(row, col, newMeshSize);
					}
				}
			}
		}
	}

	/*
	Desc: Sets the UV values based on their percentage of the heightFactor

	parameters:


	Returns:

	Pre: 
	uvs is initialized;
	heightFactor != 0

	*/
	void SetHeightBasedUVs() {
		foreach (Vector3 vert in vertices) {
			if (vert.y < 0) {
				if (vert.y < MaxNegativeHeight) MaxNegativeHeight = vert.y;
			}
			else {
				if (vert.y > MaxPositiveHeight) MaxPositiveHeight = vert.y;
			}
		}

		float uv;
		for (int i = 0; i < Size * Size; i++) {
			if (vertices[i].y >= 0) {
				uv = Mathf.Lerp(0.5f, 1f, vertices[i].y / MaxPositiveHeight);
			}
			else {
				uv = Mathf.Lerp(0.5f, 0f, vertices[i].y / MaxNegativeHeight);
			}
			uvs[i] = new Vector2(uv, uv);
		}
	}

	/*
	Desc: Reassigned the arrays to the subMeshes so Unity will redraw them

	parameters:


	Returns:

	Pre:
	All subMeshStructs in the List are initialized with non-null values

	*/
	void RebuildMesh() {
		foreach (subMeshStruct subMesh in subMeshes) {
			subMesh.subMesh.Clear();
			SetSubMeshArrays(subMesh.subMesh, subMesh.row, subMesh.col);
			subMesh.subMesh.RecalculateBounds();
			subMesh.subMesh.RecalculateNormals();
			subMesh.collider.sharedMesh = subMesh.subMesh;
		}
	}

	/*
	Desc: Creates all the wave generators the simulate an ocean

	parameters:


	Returns:

	Pre:
	water is initialized
	water objects have a WaveGenerator Component

	*/
	void BuildOcean() {

		//Find the wave generator vertex distance based on the number of expansions
		int vertDist = (int)Mathf.Pow(2, fractalExpansions + 1);

		//Create a new wave generator at the origin with a rotation
		GameObject waterObj = Instantiate(water, new Vector3(Size / 2f, 0.5f, Size / 2f), Quaternion.identity, transform);
		WaveGenerator wave = waterObj.GetComponent<WaveGenerator>();

		//Set the wave generator's values
		wave.size = subMeshSize + 1;
		wave.vertexDistance = vertDist;


		waterObj = Instantiate(water, new Vector3(Size / 2f, 0.5f, Size / 2f), Quaternion.Euler(0, 90f, 0), transform);
		wave = waterObj.GetComponent<WaveGenerator>();
		wave.size = subMeshSize + 1;
		wave.vertexDistance = vertDist;

		waterObj = Instantiate(water, new Vector3(Size / 2f, 0.5f, Size / 2f), Quaternion.Euler(0, 180f, 0), transform);
		wave = waterObj.GetComponent<WaveGenerator>();
		wave.size = subMeshSize + 1;
		wave.vertexDistance = vertDist;

		waterObj = Instantiate(water, new Vector3(Size / 2f, 0.5f, Size / 2f), Quaternion.Euler(0, -90f, 0), transform);
		wave = waterObj.GetComponent<WaveGenerator>();
		wave.size = subMeshSize + 1;
		wave.vertexDistance = vertDist;

	}

	/*
	Desc: Find the average of the vertices around the given on 
	on diagonals. Then find the max deviation and adds it to the
	average height based on a randomization factor

	parameters:
	int row: The row of the vertex to find the average of 
	int col: The column of the vertex to find the average of

	Returns:
	float: The new height value to be used.
	*/
	float CalculateDiagonalValue(int row, int col) {
		//Get all the neighbors values
		float nwNeighbor = vertices[((row + 1) * Size) + col - 1].y;
		float neNeighbor = vertices[((row + 1) * Size) + col + 1].y;
		float seNeighbor = vertices[((row - 1) * Size) + col + 1].y;
		float swNeighbor = vertices[((row - 1) * Size) + col - 1].y;

		//Average them
		float average = (nwNeighbor + neNeighbor + seNeighbor + swNeighbor) / 4;

		//Find the max deviation
		float maxDeviation = 0;
		if (Mathf.Abs(average - nwNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - nwNeighbor);
		if (Mathf.Abs(average - neNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - neNeighbor);
		if (Mathf.Abs(average - swNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - swNeighbor);
		if (Mathf.Abs(average - seNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - seNeighbor);

		//Add the randomization fact
		float deviation = maxDeviation * randomizationFactor;

		return (average + UnityEngine.Random.Range(-deviation, deviation));


	}

	/*
	Desc: Find the average of the vertices around the given on 
	on cardinals. Then find the max deviation and adds it to the
	average height based on a randomization factor

	parameters:
	int row: The row of the vertex to find the average of 
	int col: The column of the vertex to find the average of

	Returns:
	float: The new height value to be used.

	Pre:
	0 < row < max
	0 < col < max
	size > 1

	*/
	float CalculateCardinalValue(int row, int col, int max) {
		float nNeighbor;
		float sNeighbor;
		float eNeighbor;
		float wNeighbor;

		//The number of values in the average, reduced if there are out
		// of bounds points
		int values = 4;

		//The following block checks for out of bounds points and set the values
		//to either zero or the height

		if (row + 1 >= max) {
			values--;
			nNeighbor = 0;
		}
		else nNeighbor = vertices[((row + 1) * Size) + col].y;

		if (row - 1 < 0) {
			values--;
			sNeighbor = 0;
		}
		else sNeighbor = vertices[((row - 1) * Size) + col].y;

		if (col + 1 >= max) {
			values--;
			eNeighbor = 0;
		}
		else eNeighbor = vertices[((row * Size)) + col + 1].y;

		if (col - 1 < 0) {
			values--;
			wNeighbor = 0;
		}
		else wNeighbor = vertices[(row * Size) + col - 1].y;


		//Finds the average
		float average = (nNeighbor + sNeighbor + eNeighbor + wNeighbor) / values;


		//Find the maximum deviation for points in bounds
		float maxDeviation = 0;
		if (row + 1 < max) {
			if (Mathf.Abs(average - nNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - nNeighbor);
		}
		if (row - 1 >= 0) {
			if (Mathf.Abs(average - sNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - sNeighbor);
		}
		if (col + 1 < max) {
			if (Mathf.Abs(average - eNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - eNeighbor);
		}
		if (col - 1 >= 0) {
			if (Mathf.Abs(average - wNeighbor) > maxDeviation) maxDeviation = Mathf.Abs(average - wNeighbor);
		}

		//Calculates the deviation with a random element
		float deviation = maxDeviation * randomizationFactor;

		return average + UnityEngine.Random.Range(-deviation, deviation);
	}
}
