using UnityEngine;

[RequireComponent(typeof(Rigidbody))]


/*
	Author: Nicholas Edwards #3030212

	Created: April 10, 2017

	Description: This is a simple controller that simulates the
	movement of an airplane
*/
public class PlaneController : MonoBehaviour {

	float maxSpeed;
	public float TURN_SPEED = 30f;
	public float speedToSizeRatio = 0.02f;

	private Rigidbody physics;

	public FractalTerrain terrain;


	// Initializes the rigidbody and sets a starting speed
	void Start() {
		physics = transform.GetComponent<Rigidbody>();

		if (terrain == null) Debug.Log("impossible");

		//Look down at the so the player can get their barings
		transform.position = new Vector3(0, terrain.maxPositiveHeight * 1.5f, 0);
		transform.rotation = Quaternion.Euler(90, 0, 0);

		//Set our max speed based on how large the terrain is
		maxSpeed = terrain.size * speedToSizeRatio;

		//Initialize our velocity
		physics.velocity = maxSpeed * transform.forward;
	}

	// Processes the rotations, then
	// processes the target speed
	void FixedUpdate() {

		transform.Rotate(Input.GetAxis("Vertical") * TURN_SPEED * Time.fixedDeltaTime, -Input.GetAxis("Yaw") * TURN_SPEED * Time.fixedDeltaTime, -Input.GetAxis("Horizontal") * TURN_SPEED * Time.fixedDeltaTime);

		physics.velocity = physics.velocity.magnitude * transform.forward;

	}

	/*
	Desc: Determines if we collided with terrain and if so, resets

	parameters:
	Collision col: The collision object for the collision

	*/
	void OnCollisionEnter(Collision col) {
		if (col.collider.tag == "Terrain" || col.collider.tag == "Water") {
			Reset();
		}
	}


	/*
	Desc: Resets the position and rotation to be above the terrain and looking down
	Then initializes the speed again

	parameters:
	

	Returns:
	
	*/
	void Reset() {
		transform.rotation = Quaternion.Euler(90, 0, 0);
		transform.position = new Vector3(0, terrain.maxPositiveHeight * 1.5f, 0);
		physics.velocity = maxSpeed * transform.forward;
	}

}
