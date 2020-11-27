using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This component is responsible for showing the correct sprite depending on the Player state
public class PlayerSprites : MonoBehaviour {

	// The five different sprites
	public GameObject playerGoRightSprite;		// GameObject - Because it has its own collider
	public GameObject playerGoLeftSprite;		// GameObject - Because it has its own collider
	public SpriteRenderer playerDeathSprite;
	public Sprite playerLeftSadSprite;
	public Sprite playerRightSadSprite;

	private Player player;
	
    void Start() {
        player = FindObjectOfType<Player>();
    }
	
	// First fetches Player states, then sets correct sprite
    public void UpdateSprite(){
		bool isGoingRight = player.IsGoingRight();
		bool isHappy = player.IsHappy();
		bool isAlive = player.IsAlive();

		if(!isAlive){
			playerGoLeftSprite.SetActive(false);
			playerGoRightSprite.SetActive(false);
			playerDeathSprite.enabled = true;
			return;
		}

		if(!isHappy){
			playerGoLeftSprite.GetComponent<SpriteRenderer>().sprite = playerLeftSadSprite;
			playerGoRightSprite.GetComponent<SpriteRenderer>().sprite = playerRightSadSprite;
		}
		
		playerGoLeftSprite.SetActive(!isGoingRight);
		playerGoRightSprite.SetActive(isGoingRight);
	}
}
