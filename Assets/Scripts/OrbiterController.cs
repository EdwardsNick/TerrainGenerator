using UnityEngine;

/*
	Author: Nicholas Edwards #3030212

	Created: April 10, 2017

	Description: This class orbits the terrain at the height
	large enough the see the entire terrain
*/
public class OrbiterController : MonoBehaviour {

	float currentRotation = 0f; //The current angle of rotation around the map
	public FractalTerrain terrain; //A refernce to the terrain

	//The radius and height of the circle we will orbit around
	float radius;
	float height;

	// Use this for initialization
	void Start() {

		//set these values based on the terrain size
		radius = terrain.Size / 4f;
		height = terrain.MaxPositiveHeight * 1.5f;
	}

	// Update is called once per frame
	void Update() {
		//set of rotation back to zero (may not be neccesary...?)
		if (currentRotation > 359) {
			currentRotation = 0;
		}

		//Sets our position based on our rotation
		transform.position = new Vector3(radius * Mathf.Sin(currentRotation), height, radius * Mathf.Cos(currentRotation));

		//Aims the camera at the origin or the center of the map
		transform.LookAt(Vector3.zero);

		//Increments the rotation angle
		currentRotation += Time.deltaTime / 3f;
	}
}
