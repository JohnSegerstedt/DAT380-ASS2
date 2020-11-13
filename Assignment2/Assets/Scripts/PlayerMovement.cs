using UnityEngine;

public class PlayerMovement : MonoBehaviour {


	public GameObject playerGoRightSprite;
	public GameObject playerGoLeftSprite;

	private float playerSidewaysForce = 5f;
	private float playerUpwardsForce = 300f;

	private float playerMaxSpeed = 5f;

	private bool playerInAir = false;
	
	private KeyCode leftKey = KeyCode.A;
	private KeyCode rightKey = KeyCode.D;
	private KeyCode jumpKey = KeyCode.Space;

	private Rigidbody2D playerRigidBody;


    // Start is called before the first frame update
    void Start(){
        playerRigidBody = transform.GetComponent<Rigidbody2D>();
    }


    void Update(){

		if(Input.GetKey(leftKey)){
			if(!Input.GetKey(rightKey)){
				if(playerRigidBody.velocity.x > -playerMaxSpeed){
					playerRigidBody.AddForce(Vector2.left * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					playerGoRightSprite.SetActive(false);
					playerGoLeftSprite.SetActive(true);
				}
			}
		}
		if(Input.GetKey(rightKey)){
			if(!Input.GetKey(leftKey)){
				if(playerRigidBody.velocity.x < playerMaxSpeed){
					playerRigidBody.AddForce(Vector2.right * playerSidewaysForce * (playerInAir ? 0.3f : 1f));
					playerGoRightSprite.SetActive(true);
					playerGoLeftSprite.SetActive(false);
				}
			}
		}
        
		if(Input.GetKeyDown(jumpKey)){
			if(!playerInAir){
				playerInAir = true;
				playerRigidBody.AddForce(Vector2.up * playerUpwardsForce);
			}
		}
    }

	public void TouchedGround(){
		playerInAir = false;
	}

}
