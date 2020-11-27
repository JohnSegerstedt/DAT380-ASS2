using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour{

	private float rotateSpeed = 20f;


	private void Update() {
		this.gameObject.transform.Rotate(0f, 0f, Time.deltaTime * rotateSpeed);
	}

}
