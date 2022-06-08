using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinectManagementScript : MonoBehaviour {
	public static int patternNumber=1;

	void Start () {
		//KM = GameObject.FindGameObjectWithTag("Kinect");
		//다음씬으로 넘어가도 오브젝트 유지하게 해줌
		DontDestroyOnLoad(gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
