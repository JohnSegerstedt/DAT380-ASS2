using UnityEngine;

// Handles user input and applies forces to the player game object
public class PlayerMovement : MonoBehaviour {

	private float playerSidewaysForce = 1000f;
	private float playerUpwardsForce = 300f;

	private float playerMaxSpeed = 5f;
	private KeyCode leftKey = KeyCode.A;
	private KeyCode rightKey = KeyCode.D;
	private KeyCode jumpKey = KeyCode.Space;
	

	private bool playerInAir = false;

	public Player player;
	public GameManagerScript gameManager;
	private Rigidbody2D playerRigidBody;


    // Start is called before the first frame update
    void Start(){
		player = FindObjectOfType<Player>();
        playerRigidBody = transform.GetComponent<Rigidbody2D>();
    }


    void Update(){

		if(!gameManager.IsGameAlive()) return;

		if(Input.GetKey(leftKey)){ // Go Left
			if(!Input.GetKey(rightKey)){
				if(playerRigidBody.velocity.x > -playerMaxSpeed){
					playerRigidBody.AddForce(Time.deltaTime * Vector2.left * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					player.GoLeft();
				}
			}
		}
		if(Input.GetKey(rightKey)){ // Go Right
			if(!Input.GetKey(leftKey)){
				if(playerRigidBody.velocity.x < playerMaxSpeed){
					playerRigidBody.AddForce(Time.deltaTime * Vector2.right * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					player.GoRight();
				}
			}
		}
        
		if(Input.GetKeyDown(jumpKey)){ // Jump
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



}
