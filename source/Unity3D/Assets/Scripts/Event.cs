
#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Event : MonoBehaviour
{
	

	public GameObject MainCamera, MenuPanel;
	public GameObject Cat, CatMesh;
	public GameObject Laserpoint;
	public GameObject emo, food, hand;
	public GameObject maincamera, subcamera;
	public Animator anim, anim_hand;
	public Cat catScript;
	public static Vector3 rightWristVector;
	public bool userInput = false;
	public bool bInput = false;

	new AudioSource audio;
	private bool actionTrigger = false, MenuActivate = false;
	public Slider FriendlySlider, HungerSlider, StatusSlider;
	public float actionTime, currentTime, cameraTime;
	public int value = 0, rand = 0;
	protected int AuCounter = 0;


	public void GestureInProgress (uint userId, int userIndex, KinectGestures.Gestures gesture,
	                               float progress, KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos)
	{
		// don't do anything here
	}

	public bool GestureCompleted (uint userId, int userIndex, KinectGestures.Gestures gesture,
	                              KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos)
	{
		return true;
	}

	public bool GestureCancelled (uint userId, int userIndex, KinectGestures.Gestures gesture,
	                              KinectWrapper.NuiSkeletonPositionIndex joint)
	{
		// don't do anything here, just reset the gesture state
		return true;
	}

	private void Awake ()
	{
		MenuPanel = GameObject.Find ("MenuPanel");
	}

	// Use this for initialization
	void Start ()
	{
		subcamera.SetActive (true);
		maincamera.SetActive (true);
	
		subcamera.GetComponent<Camera> ().depth = 0;
		maincamera.GetComponent<Camera> ().depth = 1;
		hand.SetActive (false);
		//hand.GetComponent<Renderer>().enabled = W

		CatMesh.GetComponent<Renderer> ().material.mainTexture = Resources.Load ("cu_cat2_" + PatternCursor.patternNum) as Texture2D;
		Laserpoint = GameObject.Find ("HitPoint");
		food = GameObject.Find ("Can_1");
		catScript = Cat.GetComponent<Cat> ();
		anim = Cat.GetComponent<Animator> ();
		anim_hand = hand.GetComponent<Animator> ();
		emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("idle") as Texture2D;
		emo.SetActive (true);
		MainCamera = GameObject.Find ("Main Camera");
		actionTime = Time.time;
		food.SetActive (false);
		cameraTime = 0.0f;
		Laserpoint.GetComponent<LineRenderer> ().enabled = false;
		KinectGestures.initialBodyTrigger = true;
		//SoundControl.instance.playLoop ();
	}
	// Update is called once per frame
	void Update ()
	{
		if (!KinectManager.AuToken) {
			Debug.Log (Laserpoint.GetComponent<LineRenderer> ().enabled);
			KinectManager kinectManager = KinectManager.Instance;
			if ((!kinectManager || !kinectManager.IsInitialized () || !kinectManager.IsUserDetected ())) {
				Debug.Log ("no it doesnt");
				if (!kinectManager.IsUserDetected ()) {
					AuCounter++;
				}
			} else {
				if (bInput == false && value == 0 && !Laserpoint.GetComponent<LineRenderer> ().enabled) {
					SetUserInput (true);
				}
				uint userId = kinectManager.GetPlayer1ID ();
				AuCounter = 0;
				kinectManager.DetectGesture (userId, KinectGestures.Gestures.RaiseRightHand);
			}

			//counter++;
			MenuActivate = MenuPanel.activeSelf;
			if (currentTime - cameraTime > 2.0f && cameraTime != 0.0f) {
				subcamera.GetComponent<Camera> ().depth = 0;
				maincamera.GetComponent<Camera> ().depth = 1;
				cameraTime = 0.0f;
			}

			if (MenuActivate) {
				MainCamera.GetComponent<AudioSource> ().volume = 0.1f;
				//KinectManagementScript.GetComponent<AudioSource>().volume = 0.1f;
				SetUserInput (false);
				bInput = false;
				actionTrigger = false;
			} else {
				MainCamera.GetComponent<AudioSource> ().volume = 0.4f;
				//KinectManagementScript.GetComponent<AudioSource>().volume = 0.4f;
			}


#if (DEBUG)
			string t = Server.returnValue; // "123.456" -> 123.456 
			if (t != "") {
				t = t.Substring (0, t.Length - 5);
				float a;
				if (float.TryParse (t, out a)) {
					if (a <= (float)1.2f && a >= (float)0.8f) {
						bInput = true;
						value = 1;
					} else if (a <= (float)2.3f && a >= (float)1.5f) {
						bInput = true;
						value = 2;
					} else if (a <= (float)3.3f && a >= (float)2.7f) {
						bInput = true;
						value = 3;
					} else if (a <= (float)4.1f && a >= (float)3.9f) {
						value = 4;
					} else {
						bInput = false;
						value = 0;
					}

					/*
				if (Input.GetKeyDown ("1")) {
					bInput = true;
					value = 1;
				} else if (Input.GetKeyDown ("2")) {
					bInput = true;
					value = 2;
				} else if (Input.GetKeyDown ("3")) {
					bInput = true;
					value = 3;
				}
                */

				}

				CatResponse (value);

			} else {
				bInput = false;
				value = 0;
			}
			//Debug.Log ("sss"+value);
#endif
			/*
		if (Laserpoint.GetComponent<LineRenderer> ().enabled) {
			Ray Rays = Camera.main.ScreenPointToRay(Input.mousePosition);
			Debug.Log (Input.mousePosition);
			RaycastHit Hit;
			if (Physics.Raycast(Rays, out Hit, Mathf.Infinity))
			{
				Laserpoint.transform.position = Hit.point;
				Laserpoint.GetComponent<LineRenderer>().SetPosition(0, Laserpoint.transform.position);
				Laserpoint.GetComponent<LineRenderer>().SetPosition(1, GameObject.Find ("LaserStart").transform.position);
			}
		}
		*/
        
			currentTime = Time.time;

			if (Laserpoint.GetComponent<LineRenderer> ().enabled) {
				Vector3 temp;
				rightWristVector = KinectGestures.rightWristVector;

				//Debug.Log (Camera.main.pixelWidth + ", " + Camera.main.pixelHeight);
				temp.x = Camera.main.pixelWidth / 2 + rightWristVector.x * Camera.main.pixelWidth;
				temp.y = rightWristVector.y * Camera.main.pixelHeight - Camera.main.pixelHeight;
				temp.z = -10;

				Ray Rays = Camera.main.ScreenPointToRay (temp);
				RaycastHit Hit;
				if (Physics.Raycast (Rays, out Hit, Mathf.Infinity)) {
					Laserpoint.transform.position = Hit.point;
					Laserpoint.GetComponent<LineRenderer> ().SetPosition (0, Laserpoint.transform.position);
					Laserpoint.GetComponent<LineRenderer> ().SetPosition (1, GameObject.Find ("LaserStart").transform.position);
				}


				if (System.Math.Abs (Laserpoint.transform.position.x - GameObject.Find ("Sphere").transform.position.x) < 1.5) {
					if (System.Math.Abs (Laserpoint.transform.position.y - GameObject.Find ("Sphere").transform.position.y) < 1.5) {
						catScript.set_select_behavior ((int)CatState.play);
						emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("love") as Texture2D;
					} else if (System.Math.Abs (Laserpoint.transform.position.z - GameObject.Find ("Sphere").transform.position.z) < 1.5) {
						catScript.set_select_behavior ((int)CatState.play);
						emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("love") as Texture2D;
					}
				} else if (System.Math.Abs (Laserpoint.transform.position.x - GameObject.Find ("prop_pole_model").transform.position.x) < 0.5) {
					if (System.Math.Abs (Laserpoint.transform.position.y - GameObject.Find ("prop_pole_model").transform.position.y) < 0.5) {
						catScript.set_select_behavior ((int)CatState.pole);
					} else if (System.Math.Abs (Laserpoint.transform.position.z - GameObject.Find ("prop_pole_model").transform.position.z) < 0.5) {
						catScript.set_select_behavior ((int)CatState.pole);
					}
				} else {
					catScript.set_select_behavior ((int)CatState.walk);
					catScript.agent.SetDestination (Laserpoint.transform.position);
				}
			}

#if (DEBUG)
			//
#else
        KinectManager kinectManager = KinectManager.Instance;
        if (kinectManager.IsUserDetected())
        {
			if (maincamera.GetComponent<AudioSource> ().isPlaying)
                subcamera.GetComponent<AudioSource>().time = maincamera.GetComponent<AudioSource>().time;
            subcamera.SetActive(true);
            maincamera.SetActive(false);
       }
		if ((!kinectManager || !kinectManager.IsInitialized () || !kinectManager.IsUserDetected ())) {
			SetUserInput (false);
			if (subcamera.GetComponent<AudioSource> ().isPlaying)
                maincamera.GetComponent<AudioSource>().time = subcamera.GetComponent<AudioSource>().time;
            subcamera.SetActive(false);
           	maincamera.SetActive(true);           
        }
        else
        {
			//if (!Laserpoint.GetComponent<LineRenderer> ().enabled) {
			uint userId = kinectManager.GetPlayer1ID ();
			kinectManager.DetectGesture (userId, KinectGestures.Gestures.RaiseRightHand);
			kinectManager.DetectGesture (userId, KinectGestures.Gestures.RaiseLeftHand);
			if (Laserpoint.GetComponent<LineRenderer> ().enabled == false) {
				if (currentTime - actionTime >= 5.0f)
					SetUserInput (true);
				string t = Server.returnValue; // "123.456" -> 123.456 
				if (t != "") {
					t = t.Substring (0, t.Length - 5);
					float a;
					int b = 0;
					if (float.TryParse (t, out a)) {
						if (a < (float)2.2) {
							//쓰다듬기 임계
							b = 1;
						} else {
							if (a < (float)2.8) {
								//박수 임계
								b = 2;
							} else {
								b = 3;
							}
						}
						//Debug.Log (a + " : " + b);
						actionTrigger = true;
						SetUserInput (true);                        
                        CatResponse (b); 
					}
				}
			}
		}
#endif

			GameObject light = GameObject.Find ("Directional Light");
			light.transform.Rotate (0, 5 * Time.deltaTime, 0);
			if (light.transform.rotation.x >= 0.6f || light.transform.rotation.x <= -0.6) {
				light.GetComponent<Light> ().intensity = 0;
			} else {
				light.GetComponent<Light> ().intensity = 1;
			}
			userInput = anim.GetBool ("UserInput");

			if (AuCounter > 300) {
				KinectManager.AuToken=true;
				SceneManager.LoadScene ("Initialize");
			}
		} 
		else {
			//Authenticate
		}
	}

	public void SetActionTrigger ()
	{
		actionTrigger = true;
	}

	public void SetUserInput (bool t)
	{
		anim.SetBool ("UserInput", t);
	}

	public void CatResponse (int b)
	{
#if (DEBUG)
		if (true) {
#else
		if(!Laserpoint.GetComponent<LineRenderer> ().enabled){
#endif

#if (DEBUG)
			//if (bInput)
			if (b == 4) {
				MenuPanel.SetActive (true);
				value = 0;
			} else {
				if (bInput && userInput && !Laserpoint.GetComponent<LineRenderer> ().enabled)
#else
			if (anim.GetBool("UserInput")|| actionTrigger)
#endif
            {
					//bInput = false;
					Debug.Log (b + " " + bInput);
					//result = catFriendly();
					actionTrigger = false;
					emo.SetActive (true);
					anim.SetBool ("Run", false);
					anim.SetBool ("Walk", false);
					anim.SetBool ("Sleep", false);
					anim.SetBool ("Play", false);
					anim.SetBool ("Wash", false);
					anim.SetBool ("B_idle", false);
					anim.SetBool ("Hungry", false);
					SetUserInput (false);

					//쓰다듬기
					if (b == 1) {
						hand.SetActive (true);   //손 나타나게
						//카메라 전환
						subcamera.GetComponent<Camera> ().depth = 1;
						maincamera.GetComponent<Camera> ().depth = 0;
						//subcamera.SetActive (true);
						//maincamera.SetActive(false);
						hand.SetActive (true);

						anim_hand.SetTrigger ("Happy");  //손 애니메이션 시작
						anim.SetTrigger ("Positive");    //고양이 애니메이션 

						emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("happy") as Texture2D;
						SoundControl.instance.playM2Sound ();

						value = 0;
					}
                //박수치기 
                else if (b == 2) {
						subcamera.GetComponent<Camera> ().depth = 1;
						maincamera.GetComponent<Camera> ().depth = 0;
						hand.SetActive (false);
						anim.SetTrigger ("Nagative");
						emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("angry") as Texture2D;
						SoundControl.instance.playHsSound ();

						value = 0;
					}
                //응원하기
                else if (b == 3) {
						subcamera.GetComponent<Camera> ().depth = 1;
						maincamera.GetComponent<Camera> ().depth = 0;
						anim.SetTrigger ("Chodai");
						anim.SetTrigger ("EarStart");
						hand.SetActive (false);
						anim.SetBool ("EarDown", true);
						anim.SetBool ("EarUp", true);
						emo.GetComponent<MeshRenderer> ().material.mainTexture = Resources.Load ("love") as Texture2D;
						SoundControl.instance.playNySound ();

						value = 0;
					}               
					/*
					food.SetActive(true);
					if (catScript.hungry <= 80)
					{
						anim.SetTrigger("Eat");
						catScript.hungry += 50.0f;
						emo.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load("yammy") as Texture;
					}
					else
					{
						anim.SetBool("UserInput", false);
						emo.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load("embarrass") as Texture2D;
					}
				*/
					//	subcamera.SetActive (false);
					//	maincamera.SetActive(true);
					//SoundControl.instance.playLoop ();
					Server.returnValue = "";
					actionTime = Time.time;
					SetUserInput (true);
					//SetUserInput(false);
					bInput = false;
					actionTrigger = false;
					cameraTime = Time.time;
				}
			}
		}
	}
	/*
	public void LaserToggle(){
		if(Laserpoint.GetComponent<LineRenderer>().enabled)
		{
			Laserpoint.GetComponent<LineRenderer>().enabled = false;
			SetUserInput(true);
			bInput = true;
			Debug.Log("Laser False");
		}
		else if (!Laserpoint.GetComponent<LineRenderer>().enabled)
		{
			Laserpoint.GetComponent<LineRenderer>().enabled = true;
			SetUserInput(false);
			bInput = false;
			Debug.Log("Laser True");
			MenuPanel.SetActive (false);
		}
	}*/
}