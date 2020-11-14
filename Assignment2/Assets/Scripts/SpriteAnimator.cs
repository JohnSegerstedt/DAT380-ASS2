using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimator : MonoBehaviour{ 

	[SerializeField] private Sprite[] frameArray;
	private int currentFrame;
	private float timer;

	[SerializeField] private float timePerFrame = 0;

	private SpriteRenderer spriteRenderer;

	private void Start() {
	if(timePerFrame == 0) timePerFrame = 0.1f;
		spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
	}

	// Update is called once per frame
	void Update(){
        timer += Time.deltaTime;

		if(timer >= timePerFrame){
			timer -= timePerFrame;
			currentFrame++;
			if(currentFrame >= frameArray.Length){
				currentFrame = 0;
			}
			spriteRenderer.sprite = frameArray[currentFrame];
		}
    }
}
