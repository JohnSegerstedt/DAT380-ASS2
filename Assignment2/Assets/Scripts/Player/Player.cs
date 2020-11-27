using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The main Player data type
// When updated, calls 'UpdateListeners'
// Could be a made into a Observer pattern if necessary
// (Currently only one listener)
public class Player : MonoBehaviour {

	private bool isGoingRight = true;
	private bool isHappy = true;
	private bool isAlive = true;

	private PlayerSprites playerSprites;
	private GameManagerScript gameManager;

	public void Start() {
		playerSprites = FindObjectOfType<PlayerSprites>();
		gameManager = FindObjectOfType<GameManagerScript>();
	}


	public bool IsGoingLeft(){
		return !isGoingRight;
	}

	public bool IsGoingRight(){
		return isGoingRight;
	}

	public bool IsHappy(){
		return isHappy;
	}

	public bool IsAlive(){
		return isAlive;
	}

	public void GoRight(){
		if (isGoingRight) return;
		isGoingRight = true;
		UpdateListeners();
	}

	public void GoLeft(){
		if(!isGoingRight) return;
		isGoingRight = false;
		UpdateListeners();
	}
	
	public void MakeSad(){
		if(!isAlive) return;
		isHappy = false;
		UpdateListeners();
	}

	public void Die(){
		if(isAlive){
			FindObjectOfType<AudioManager>().PlaySound("death");
			isAlive = false;
			UpdateListeners();
			gameManager.SetGameOver();
		}
	}

	private void UpdateListeners(){
		playerSprites.UpdateSprite();
	}

}
