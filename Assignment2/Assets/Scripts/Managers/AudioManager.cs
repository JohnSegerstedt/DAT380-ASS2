using UnityEngine.Audio;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles all audio behaviour
// To play a sound call: "FindObjectOfType<AudioManager>().PlaySound("NameOfAudio");
// credit to: https://youtu.be/6OT43pvUyfY
public class AudioManager : MonoBehaviour {

	public Sound[] sounds;      // Array sounds effects and music
	
	// Called before Start()
	void Awake () { 
		foreach(Sound sound in sounds) {
			// Links editor's Sound variables to audio's source
			sound.source = gameObject.AddComponent<AudioSource>();
			sound.source.clip = sound.clip;
			sound.source.volume = sound.volume;
			sound.source.pitch = sound.pitch;
			sound.source.loop = sound.loop;
		}
	}

	// Plays the main theme on start
	public void Start() {
		PlaySound("theme");
	}

	// Play a sound with the given name
	public void PlaySound (string name) {
		PlaySound(name, true);
	}

	// Play a sound with the given name
	public void PlaySound (string name, bool start) {
		if(name.Equals("")) return;
		Sound s = Array.Find(sounds, sound => sound.name == name);	// Try to find name
		if (s == null) {
			Debug.LogWarning("Sound: '"+name+"' not found in 'PlaySound(...) !");
			return; 
		}
		if(start)	s.source.Play(); // Play the audio clip!
		else		s.source.Stop(); // Stop the audio clip!
	}




}
