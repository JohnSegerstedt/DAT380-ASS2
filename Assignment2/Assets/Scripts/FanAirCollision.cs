using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanAirCollision: MonoBehaviour{

	private float airForce = 20f;
	private Vector2 direction;

	private void Start() {
		float rotation = GetComponentInParent<Transform>().rotation.eulerAngles.z;
		direction = new Vector2((float) -Math.Cos(rotation*Mathf.PI/180f), (float) -Math.Sin(rotation*Mathf.PI/180f));
	}
	

	private void OnTriggerStay2D(Collider2D collider) {
		try{
			collider.GetComponent<Rigidbody2D>().AddForce(direction * airForce);
		}
		catch(Exception){}

	}
}
