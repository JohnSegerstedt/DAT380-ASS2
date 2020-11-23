using UnityEngine;

public class PlayerMovement : MonoBehaviour {


	public GameObject playerGoRightSprite;
	public GameObject playerGoLeftSprite;
	public SpriteRenderer playerDeathSprite;
	public Sprite playerLeftSadSprite;
	public Sprite playerRightSadSprite;


	private float playerSidewaysForce = 1000f;
	private float playerUpwardsForce = 300f;

	private float playerMaxSpeed = 5f;

	private bool playerInAir = false;
	
	private KeyCode leftKey = KeyCode.A;
	private KeyCode rightKey = KeyCode.D;
	private KeyCode jumpKey = KeyCode.Space;
	
	public GameManagerScript gameManager;
	private Rigidbody2D playerRigidBody;
	private bool isDead = false;


    // Start is called before the first frame update
    void Start(){
        playerRigidBody = transform.GetComponent<Rigidbody2D>();
		gameManager = FindObjectOfType<GameManagerScript>();
    }


    void Update(){

		if(!gameManager.IsGameAlive()) return;

		if(Input.GetKey(leftKey)){
			if(!Input.GetKey(rightKey)){
				if(playerRigidBody.velocity.x > -playerMaxSpeed){
					playerRigidBody.AddForce(Time.deltaTime * Vector2.left * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					playerGoRightSprite.SetActive(false);
					playerGoLeftSprite.SetActive(true);
				}
			}
		}
		if(Input.GetKey(rightKey)){
			if(!Input.GetKey(leftKey)){
				if(playerRigidBody.velocity.x < playerMaxSpeed){
					playerRigidBody.AddForce(Time.deltaTime * Vector2.right * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					playerGoRightSprite.SetActive(true);
					playerGoLeftSprite.SetActive(false);
				}
			}
		}
        
		if(Input.GetKeyDown(jumpKey)){
			if(!playerInAir){
				FindObjectOfType<AudioManager>().PlaySound("jump");
				playerInAir = true;
				playerRigidBody.AddForce(Vector2.up * playerUpwardsForce);
			}
		}
    }

	public void TouchedGround(){
		playerInAir = false;
	}

	public void Die(){
		if(!isDead){
			FindObjectOfType<AudioManager>().PlaySound("death");
			isDead = true;
			playerGoLeftSprite.SetActive(false);
			playerGoRightSprite.SetActive(false);
			playerDeathSprite.enabled = true;
			gameManager.SetGameOver();
		}
	}

	public void MakeSad(){
		if(isDead) return;
		if(playerGoRightSprite.activeInHierarchy)	playerGoRightSprite.GetComponent<SpriteRenderer>().sprite = playerRightSadSprite;
		else										playerGoLeftSprite.GetComponent<SpriteRenderer>().sprite = playerLeftSadSprite;
	}

}
