using UnityEngine;
using System;
using System.Collections;

public class CubemanController : MonoBehaviour
{
	public bool MoveVertically = false;
	public bool MirroredMovement = false;

	public static Vector3 rightWristVect;
	public static Vector3 leftWristVect;

	//public GameObject debugText;

	public GameObject Hip_Center;
	public GameObject Spine;
	public GameObject Shoulder_Center;
	public GameObject Head;
	public GameObject Shoulder_Left;
	public GameObject Elbow_Left;
	public GameObject Wrist_Left;
	public GameObject Hand_Left;
	public GameObject Shoulder_Right;
	public GameObject Elbow_Right;
	public GameObject Wrist_Right;
	public GameObject Hand_Right;
	public GameObject Hip_Left;
	public GameObject Knee_Left;
	public GameObject Ankle_Left;
	public GameObject Foot_Left;
	public GameObject Hip_Right;
	public GameObject Knee_Right;
	public GameObject Ankle_Right;
	public GameObject Foot_Right;

	private const int leftWristIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.WristLeft;
	private const int rightWristIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.WristRight;
	private const int leftElbowIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft;
	private const int rightElbowIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.ElbowRight;
	private const int leftShoulderIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft;
	private const int rightShoulderIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight;
	private const int hipCenterIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter;
	private const int shoulderCenterIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter;
	private const int leftHipIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft;
	private const int rightHipIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.HipRight;

	private static float startTime = 0.0f;
	private static float endTime = 0.0f;
	private static bool initialTrigger = false;

	public LineRenderer SkeletonLine;

	private GameObject[] bones;
	private LineRenderer[] lines;
	private int[] parIdxs;

	private Vector3 initialPosition;
	private Quaternion initialRotation;
	private Vector3 initialPosOffset = Vector3.zero;
	private uint initialPosUserID = 0;


	void Start ()
	{
		//store bones in a list for easier access
		bones = new GameObject[] {
			Hip_Center, Spine, Shoulder_Center, Head,  // 0 - 3
			Shoulder_Left, Elbow_Left, Wrist_Left, Hand_Left,  // 4 - 7  6
			Shoulder_Right, Elbow_Right, Wrist_Right, Hand_Right,  // 8 - 11  10
			Hip_Left, Knee_Left, Ankle_Left, Foot_Left,  // 12 - 15
			Hip_Right, Knee_Right, Ankle_Right, Foot_Right  // 16 - 19
		};

		parIdxs = new int[] {
			0, 0, 1, 2,
			2, 4, 5, 6,
			2, 8, 9, 10,
			0, 12, 13, 14,
			0, 16, 17, 18
		};

		// array holding the skeleton lines
		lines = new LineRenderer[bones.Length];

		if (SkeletonLine) {
			for (int i = 0; i < lines.Length; i++) {
				lines [i] = Instantiate (SkeletonLine) as LineRenderer;
				lines [i].transform.parent = transform;
			}
		}

		initialPosition = transform.position;
		initialRotation = transform.rotation;
		//transform.rotation = Quaternion.identity;
	}

	// Update is called once per frame
	void Update ()
	{
		if (!KinectGestures.readyTrigger || KinectManager.AuToken) {
			KinectManager manager = KinectManager.Instance;

			// get 1st player
			uint playerID = manager != null ? manager.GetPlayer1ID () : 0;

			if (playerID <= 0) {
				// reset the pointman position and rotation
				if (transform.position != initialPosition) {
					transform.position = initialPosition;
				}

				if (transform.rotation != initialRotation) {
					transform.rotation = initialRotation;
				}

				for (int i = 0; i < bones.Length; i++) {
					bones [i].gameObject.SetActive (true);

					bones [i].transform.localPosition = Vector3.zero;
					bones [i].transform.localRotation = Quaternion.identity;

					if (SkeletonLine) {
						lines [i].gameObject.SetActive (false);
					}
				}

				return;
			}

			// set the user position in space
			Vector3 posPointMan = manager.GetUserPosition (playerID);
			posPointMan.z = !MirroredMovement ? -posPointMan.z : posPointMan.z;

			// store the initial position
			if (initialPosUserID != playerID) {
				initialPosUserID = playerID;
				initialPosOffset = transform.position - (MoveVertically ? posPointMan : new Vector3 (posPointMan.x, 0, posPointMan.z));
			}

			transform.position = initialPosOffset + (MoveVertically ? posPointMan : new Vector3 (posPointMan.x, 0, posPointMan.z));

			// update the local positions of the bones
			for (int i = 0; i < bones.Length; i++) {
				if (bones [i] != null) {
					int joint = MirroredMovement ? KinectWrapper.GetSkeletonMirroredJoint (i) : i;

					if (manager.IsJointTracked (playerID, joint)) {
						bones [i].gameObject.SetActive (true);

						Vector3 posJoint = manager.GetJointPosition (playerID, joint);
						posJoint.z = !MirroredMovement ? -posJoint.z : posJoint.z;

						Quaternion rotJoint = manager.GetJointOrientation (playerID, joint, !MirroredMovement);
						rotJoint = initialRotation * rotJoint;

						posJoint -= posPointMan;

						if (MirroredMovement) {
							posJoint.x = -posJoint.x;
							posJoint.z = -posJoint.z;
						}

						bones [i].transform.localPosition = posJoint;

						bones [i].transform.rotation = rotJoint;
					} else {
						bones [i].gameObject.SetActive (false);
					}
				}	
			}

			if (SkeletonLine) {
				for (int i = 0; i < bones.Length; i++) {
					bool bLineDrawn = false;

					if (bones [i] != null) {
						if (bones [i].gameObject.activeSelf) {
							Vector3 posJoint = bones [i].transform.position;

							int parI = parIdxs [i];
							Vector3 posParent = bones [parI].transform.position;

							if (bones [parI].gameObject.activeSelf) {
								lines [i].gameObject.SetActive (true);
								//lines[i].SetVertexCount(2);
								lines [i].SetPosition (0, posParent);
								lines [i].SetPosition (1, posJoint);

								bLineDrawn = true;
							}
						}
					}	

					if (!bLineDrawn) {
						lines [i].gameObject.SetActive (false);
					}
				}
			}
			/*
			if (!initialTrigger) {
				int tecm = 0;

				//높이 검ㅏ(손)
				if (bones [6].transform.position.y > Rectangle.transform.position.y &&
				    bones [6].transform.position.z > Rectangle.transform.position.z - Rectangle.transform.localScale.z / 2 &&
				    bones [6].transform.position.z < Rectangle.transform.position.z + Rectangle.transform.localScale.z / 2 &&
				    bones [6].transform.position.x > Rectangle.transform.position.x - Rectangle.transform.localScale.x / 2 &&
				    bones [6].transform.position.x < Rectangle.transform.position.x + Rectangle.transform.localScale.x / 2) {
					tecm++;
				}


				if (bones [10].transform.position.y > Rectangle.transform.position.y &&
				    bones [10].transform.position.z > Rectangle.transform.position.z - Rectangle.transform.localScale.z / 2 &&
				    bones [10].transform.position.z < Rectangle.transform.position.z + Rectangle.transform.localScale.z / 2 &&
				    bones [10].transform.position.x > Rectangle.transform.position.x - Rectangle.transform.localScale.x / 2 &&
				    bones [10].transform.position.x < Rectangle.transform.position.x + Rectangle.transform.localScale.x / 2) {
					tecm++;
				}

				if (tecm == 2 && endTime - startTime == 0.0) {
					Debug.Log ("Initial");
					startTime = Time.time;
					//initial
				} else if (tecm == 2) {
					endTime = Time.time;
					endTime = endTime - startTime;
					CalibrationText.GetComponent<GUIText> ().text = "" + endTime;

					if (endTime > 3.0f) {
						startTime = Time.time;
						initialTrigger = true;
					}
				} else {
					startTime = 0.0f;
					endTime = 0.0f;
					initialTrigger = false;
				}
				tecm = 0;
			} else {
				/*
				 * //must be transed to Button
				float temp;
				en0dTime = Time.time;
				endTime = endTime - startTime;
				if (endTime > 6.0f && endTime < 12.0f) {
					CalibrationText.GetComponent<GUIText> ().text = "Hands spread Wide(6~12) : " + endTime;
					if (endTime > 9.0f && endTime < 10.0f) {
						KinectGestures.initialXTrigger = true;
					}
				} else if (endTime > 18.0f && endTime < 24.0f) {
					CalibrationText.GetComponent<GUIText> ().text = "Right hand up, Left hand down(18~24) : " + endTime;
					if (endTime > 21.0f && endTime < 22.0f) {
						KinectGestures.initialYTrigger = true;
					}
				} else if (endTime > 30.0f && endTime < 36.0f) {
					CalibrationText.GetComponent<GUIText> ().text = "Right hand front, Left hand back(30~36) : " + endTime;
					if (endTime > 33.0f && endTime < 34.0f) {
						KinectGestures.initialZTrigger = true;
					}
				} else if (endTime > 42.0f && endTime < 48.0f) {
					CalibrationText.GetComponent<GUIText> ().text = "Stand Attention!(42~48) : " + endTime;
					if (endTime > 45.0f && endTime < 46.0f) {
						KinectGestures.initialBodyTrigger = true;
					}
				} else if (endTime > 48.0f) {
					CalibrationText.GetComponent<GUIText> ().text = "Data Gathering Program, started";
					this.gameObject.SetActive(false);
					Rectangle.SetActive (false);
					KinectGestures.readyTrigger = true;
				} else {
					CalibrationText.GetComponent<GUIText> ().text = "Read carefully and Follow Instructions";
				}
			}
	}
	*/
		}
	}

}
