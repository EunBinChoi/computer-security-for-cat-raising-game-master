using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand_script : MonoBehaviour {

	public Animator anim;
	public GameObject hand;
	AnimatorStateInfo info;
	AnimatorTransitionInfo info2;
	// Use this for initialization
	void Start () {
		//anim.SetBool ("AnimSt", false);
	}
	
	// Update is called once per frame
	void Update () { 
		info = anim.GetCurrentAnimatorStateInfo(0);
		info2 = anim.GetAnimatorTransitionInfo(0);
		if (info.IsName ("Motion 0") && info.normalizedTime >= 0.70f) {
			Debug.Log ("false"+hand);

			hand.SetActive (false);

		}

	}
}
