using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerScript : MonoBehaviour{

	public GameObject gameOverUI;
	public GameObject levelCompleteUI;
	
	public Text timeTextShadow;
	public Text timeText;

	private int gameSceneIndex = 0;
	private bool gameAlive = true;
	private bool levelComplete = false;

	private float time = 0f;


    // Start is called before the first frame update
    void Start(){
        
    }


    void Update() {
		if(Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(gameSceneIndex); 
		if(gameAlive){
			time += Time.deltaTime;
			UpdateUI(time);
		}
    }

	private void UpdateUI(float currentTime){
		int mins = (int)(currentTime / 60f);
		int seconds = (int)(currentTime % 60f);
		string timeTextString = (mins < 10 ? "0" : "") + (mins < 0 ? "1" : mins.ToString())+":"+(seconds < 10 ? "0" : "") + (seconds < 0 ? "1" : seconds.ToString());
		timeTextShadow.text = timeTextString;
		timeText.text = timeTextString;
	}

	public bool IsGameAlive(){
		return gameAlive;
	}

	public void SetLevelComplete(){
		gameAlive = false;
		levelCompleteUI.SetActive(true);
	}

	public void SetGameOver(){
		gameAlive = false;
		gameOverUI.SetActive(true);
	}
}
