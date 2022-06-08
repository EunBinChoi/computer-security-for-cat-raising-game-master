using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundControl : MonoBehaviour {

	public AudioClip M2;
	public AudioClip Hs;
	public AudioClip Ny;
	//public AudioClip lop;
	public AudioSource myAudio;
	public static SoundControl instance;

	void Awake(){
		if (SoundControl.instance == null)
			SoundControl.instance = this;
	}

	// Use this for initialization
	void Start () {
		myAudio = GameObject.Find("cu_cat2_model").GetComponent<AudioSource>();
	}
	public void playM2Sound(){
        if (myAudio.isPlaying)
            myAudio.Stop();
		myAudio.PlayOneShot (M2);
	}
	public void playHsSound(){
        if (myAudio.isPlaying)
            myAudio.Stop();
        myAudio.PlayOneShot (Hs);
	}
	public void playNySound(){
        if (myAudio.isPlaying)
            myAudio.Stop();
        myAudio.PlayOneShot (Ny);
	}
	/*
	public void playLoop(){
		myAudio.Stop ();
		myAudio.loop = true;
		//myAudio.PlayOneShot (lop);
		myAudio.clip = Resources.Load ("Sounds/Greedy") as AudioClip;
		myAudio.Play ();	
	}*/
	
	// Update is called once per frame
	void Update () {
		if (myAudio == null) {
			myAudio = GameObject.Find("cu_cat2_model").GetComponent<AudioSource>();
		}
	}
}
