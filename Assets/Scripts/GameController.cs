using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
	Author: Nicholas Edwards #3030212

	Created: April 10, 2017

	Description: This class controls which camera and controller
	to use and handles swapping between them
*/
public class GameController : MonoBehaviour {

	[Header("Set these from editor")]
	public List<GameObject> cameraObjects; //The list of objects that have camera's to switch to
	public int currentIndex; //The current camera's index
	public bool cooldown; //Whether we can switch again
	public int cooldownTimer = 1; //How long to wait between switching

	// Use this for initialization
	void Start () {
		currentIndex = 0;

		//Set everything to not active
		foreach(GameObject obj in cameraObjects) {
			obj.SetActive(false);
		}
		//Set the first camera active
		cameraObjects[currentIndex].SetActive(true);

		cooldown = false;
	}
	
	// Update is called once per frame
	void Update () {

		//if we have camera's and not on cooldown
		if (cameraObjects.Count > 0 && !cooldown) {

			//If the Swap key was pressed swap to the next camera
			if (Input.GetAxis("Swap") > 0) {
				cameraObjects[currentIndex].SetActive(false);
				currentIndex++;
				if (currentIndex > cameraObjects.Count - 1) {
					currentIndex = 0;
				}
				cameraObjects[currentIndex].SetActive(true);
				StartCoroutine(Cooldown());
			}
		}
	}


	/*
	Desc: A coroutine to wait an interval before being able
	to switch modes again

	parameters:
	

	Returns:

	Post: 
	cooldown = false;
	
	*/
	public IEnumerator Cooldown () {
		cooldown = true;
		yield return new WaitForSeconds(cooldownTimer);
		cooldown = false;
	}
}
