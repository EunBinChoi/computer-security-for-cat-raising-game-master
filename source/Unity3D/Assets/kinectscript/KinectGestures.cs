using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class KinectGestures
{

	public interface GestureListenerInterface
	{
		// Invoked when a new user is detected and tracking starts
		// Here you can start gesture detection with KinectManager.DetectGesture()
		void UserDetected(uint userId, int userIndex);

		// Invoked when a user is lost
		// Gestures for this user are cleared automatically, but you can free the used resources
		void UserLost(uint userId, int userIndex);

		// Invoked when a gesture is in progress 
		void GestureInProgress(uint userId, int userIndex, Gestures gesture, float progress, 
			KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos);

		// Invoked if a gesture is completed.
		// Returns true, if the gesture detection must be restarted, false otherwise
		bool GestureCompleted(uint userId, int userIndex, Gestures gesture,
			KinectWrapper.NuiSkeletonPositionIndex joint, Vector3 screenPos);

		// Invoked if a gesture is cancelled.
		// Returns true, if the gesture detection must be retarted, false otherwise
		bool GestureCancelled(uint userId, int userIndex, Gestures gesture, 
			KinectWrapper.NuiSkeletonPositionIndex joint);
	}


	public enum Gestures
	{
		None = 0,
		RaiseRightHand,
		RaiseLeftHand,
		Psi,
		Tpose,
		Stop,
		Wave,
		Click,
		SwipeLeft,
		SwipeRight,
		SwipeUp,
		SwipeDown,
		RightHandCursor,
		LeftHandCursor,
		ZoomOut,
		ZoomIn,
		Wheel,
		Jump,
		Squat,
		Push,
		Pull
	}

	public struct GestureData
	{
		public uint userId;
		public Gestures gesture;
		public int state;
		public float timestamp;
		public int joint;
		public Vector3 jointPos;
		public Vector3 screenPos;
		public float tagFloat;
		public Vector3 tagVector;
		public Vector3 tagVector2;
		public float progress;
		public bool complete;
		public bool cancelled;
		public List<Gestures> checkForGestures;
		public float startTrackingAtTime;
	}

	//Trigger for timestamp
	//public static bool initialTrigger = false;
	//private static float[] initialBody;
	public static bool initialXTrigger=false;
	public static bool initialYTrigger=false;
	public static bool initialZTrigger=false;
	public static bool initialBodyTrigger=false;
	public static bool readyTrigger = false;
	public static bool AuXTrigger=false;
	public static bool AuYTrigger=false;
	public static bool AuZTrigger=false;

	private static float[,] calculateVectors = new float[6,4];
	private static float[,] saveBody = new float[200, 21];
	private static float[] beforeBody = new float[21];
	private static int centraflexurePoint = 0;
	private static float centraflexureValue =-1.0f;

	public static Vector3 rightWristVector;

	private static bool calculatingTrigger = false;
	private static float[] tempBody;
	private static int timeCountTrigger = 0;

	private static float XMaximum = 0.0f;
	private static float XMinimum = 0.0f;
	private static float YMaximum = 0.0f;
	private static float YMinimum = 0.0f;
	private static float ZMaximum = 0.0f;
	private static float ZMinimum = 0.0f;

	public static KinectManager manager = KinectManager.Instance;

	private static int frame = 0;
	private static bool frameTrigger = false;
	private static string frameBuffer = "";

	// Gesture related constants, variables and functions
	private const int leftHandIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft;
	private const int rightHandIndex = (int)KinectWrapper.NuiSkeletonPositionIndex.HandRight;
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

	private static void SetGestureJoint(ref GestureData gestureData, float timestamp, int joint, Vector3 jointPos)
	{
		gestureData.joint = joint;
		gestureData.jointPos = jointPos;
		gestureData.timestamp = timestamp;
		gestureData.state++;
	}

	private static void SetGestureCancelled(ref GestureData gestureData)
	{
		gestureData.state = 0;
		gestureData.progress = 0f;
		gestureData.cancelled = true;
	}

	public static void ClearALL(){
		XMaximum = 0.0f;
		XMinimum = 0.0f;
		YMaximum = 0.0f;
		YMinimum = 0.0f;
		ZMaximum = 0.0f;
		ZMinimum = 0.0f;

		initialXTrigger=false;
		initialYTrigger=false;
		initialZTrigger=false;
		initialBodyTrigger=false;
		readyTrigger = false;
		calculatingTrigger = false;
		timeCountTrigger = 0;
		frame = 0;
		frameTrigger = false;
		frameBuffer = "";
	}

	private static void CheckPoseComplete(ref GestureData gestureData, float timestamp, Vector3 jointPos, bool isInPose, float durationToComplete)
	{
		if(isInPose)
		{
			float timeLeft = timestamp - gestureData.timestamp;
			gestureData.progress = durationToComplete > 0f ? Mathf.Clamp01(timeLeft / durationToComplete) : 1.0f;

			if(timeLeft >= durationToComplete)
			{
				gestureData.timestamp = timestamp;
				gestureData.jointPos = jointPos;
				gestureData.state++;
				gestureData.complete = true;
			}
		}
		else
		{
			SetGestureCancelled(ref gestureData);
		}
	}

	private static void SetScreenPos(uint userId, ref GestureData gestureData, ref Vector3[] jointsPos, ref bool[] jointsTracked)
	{
		Vector3 handPos = jointsPos[rightHandIndex];
		//		Vector3 elbowPos = jointsPos[rightElbowIndex];
		//		Vector3 shoulderPos = jointsPos[rightShoulderIndex];
		bool calculateCoords = false;

		if(gestureData.joint == rightHandIndex)
		{
			if(jointsTracked[rightHandIndex] /**&& jointsTracked[rightElbowIndex] && jointsTracked[rightShoulderIndex]*/)
			{
				calculateCoords = true;
			}
		}
		else if(gestureData.joint == leftHandIndex)
		{
			if(jointsTracked[leftHandIndex] /**&& jointsTracked[leftElbowIndex] && jointsTracked[leftShoulderIndex]*/)
			{
				handPos = jointsPos[leftHandIndex];
				//				elbowPos = jointsPos[leftElbowIndex];
				//				shoulderPos = jointsPos[leftShoulderIndex];

				calculateCoords = true;
			}
		}
		else if(gestureData.joint == leftWristIndex)
		{
			if(jointsTracked[leftWristIndex] /**&& jointsTracked[leftElbowIndex] && jointsTracked[leftShoulderIndex]*/)
			{
				handPos = jointsPos[leftWristIndex];
				//				elbowPos = jointsPos[leftElbowIndex];
				//				shoulderPos = jointsPos[leftShoulderIndex];

				calculateCoords = true;
			}
		}

		if(calculateCoords)
		{
			//			if(gestureData.tagFloat == 0f || gestureData.userId != userId)
			//			{
			//				// get length from shoulder to hand (screen range)
			//				Vector3 shoulderToElbow = elbowPos - shoulderPos;
			//				Vector3 elbowToHand = handPos - elbowPos;
			//				gestureData.tagFloat = (shoulderToElbow.magnitude + elbowToHand.magnitude);
			//			}

			if(jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && 
				jointsTracked[leftShoulderIndex] && jointsTracked[rightShoulderIndex])
			{
				Vector3 neckToHips = jointsPos[shoulderCenterIndex] - jointsPos[hipCenterIndex];
				Vector3 rightToLeft = jointsPos[rightShoulderIndex] - jointsPos[leftShoulderIndex];

				gestureData.tagVector2.x = rightToLeft.x; // * 1.2f;
				gestureData.tagVector2.y = neckToHips.y; // * 1.2f;

				if(gestureData.joint == rightHandIndex)
				{
					gestureData.tagVector.x = jointsPos[rightShoulderIndex].x - gestureData.tagVector2.x / 2;
					gestureData.tagVector.y = jointsPos[hipCenterIndex].y;
				}
				else
				{
					gestureData.tagVector.x = jointsPos[leftShoulderIndex].x - gestureData.tagVector2.x / 2;
					gestureData.tagVector.y = jointsPos[hipCenterIndex].y;
				}
			}

			//			Vector3 shoulderToHand = handPos - shoulderPos;
			//			gestureData.screenPos.x = Mathf.Clamp01((gestureData.tagFloat / 2 + shoulderToHand.x) / gestureData.tagFloat);
			//			gestureData.screenPos.y = Mathf.Clamp01((gestureData.tagFloat / 2 + shoulderToHand.y) / gestureData.tagFloat);

			if(gestureData.tagVector2.x != 0 && gestureData.tagVector2.y != 0)
			{
				Vector3 relHandPos = handPos - gestureData.tagVector;
				gestureData.screenPos.x = Mathf.Clamp01(relHandPos.x / gestureData.tagVector2.x);
				gestureData.screenPos.y = Mathf.Clamp01(relHandPos.y / gestureData.tagVector2.y);
			}

			//Debug.Log(string.Format("{0} - S: {1}, H: {2}, SH: {3}, L : {4}", gestureData.gesture, shoulderPos, handPos, shoulderToHand, gestureData.tagFloat));
		}
	}

	private static void SetZoomFactor(uint userId, ref GestureData gestureData, float initialZoom, ref Vector3[] jointsPos, ref bool[] jointsTracked)
	{
		Vector3 vectorZooming = jointsPos[rightHandIndex] - jointsPos[leftHandIndex];

		if(gestureData.tagFloat == 0f || gestureData.userId != userId)
		{
			gestureData.tagFloat = 0.5f; // this is 100%
		}

		float distZooming = vectorZooming.magnitude;
		gestureData.screenPos.z = initialZoom + (distZooming / gestureData.tagFloat);
	}

	//	private static void SetWheelRotation(uint userId, ref GestureData gestureData, Vector3 initialPos, Vector3 currentPos)
	//	{
	//		float angle = Vector3.Angle(initialPos, currentPos) * Mathf.Sign(currentPos.y - initialPos.y);
	//		gestureData.screenPos.z = angle;
	//	}
	public static void calculateData(float[] temp, float time){//change with it
		//Debug.Log("__checking__");
		//bool Trigger = false;
		calculatingTrigger = true;
		//x righthandVector
		//calculateVectors [0,0] = calculateVectors [0,1];
		//calculateVectors [0,1] = calculateVectors [0,2];
		calculateVectors [0,2] = calculateVectors [0,3];
		calculateVectors [0,3] = temp [0]-beforeBody[0];
		//y righthandVector
		//calculateVectors [1,0] = calculateVectors [1,1];
		//calculateVectors [1,1] = calculateVectors [1,2];
		calculateVectors [1,2] = calculateVectors [1,3];
		calculateVectors [1,3] = temp [1]-beforeBody[1];
		//z righthandVector
		//calculateVectors [2,0] = calculateVectors [2,1];
		//calculateVectors [2,1] = calculateVectors [2,2];
		calculateVectors [2,2] = calculateVectors [2,3];
		calculateVectors [2,3] = temp [2]-beforeBody[2];

		//x lefthandVector
		//calculateVectors [3,0] = calculateVectors [3,1];
		//calculateVectors [3,1] = calculateVectors [3,2];
		calculateVectors [3,2] = calculateVectors [3,3];
		calculateVectors [3,3] = temp [13]-beforeBody[13];
		//y lefthandVector
		//calculateVectors [4,0] = calculateVectors [4,1];
		//calculateVectors [4,1] = calculateVectors [4,2];
		calculateVectors [4,2] = calculateVectors [4,3];
		calculateVectors [4,3] = temp [14]-beforeBody[14];
		//z lefthandVector
		//calculateVectors [5,0] = calculateVectors [5,1];
		//calculateVectors [5,1] = calculateVectors [5,2];
		calculateVectors [5,2] = calculateVectors [5,3];
		calculateVectors [5,3] = temp [15]-beforeBody[15];

		/*
		//if(System.Math.Abs(calculateVectors[0,3]-calculateVectors[0,2])
		Debug.Log("==================");
		Debug.Log((calculateVectors[0,3]-calculateVectors[0,2])*10000+"::::"+(calculateVectors[1,3]-calculateVectors[1,2])*10000+"::::"+(calculateVectors[2,3]-calculateVectors[2,2])*10000);
		Debug.Log((calculateVectors[3,3]-calculateVectors[3,2])*10000+"::::"+(calculateVectors[4,3]-calculateVectors[4,2])*10000+"::::"+(calculateVectors[5,3]-calculateVectors[5,2])*10000);
		Debug.Log("==================");
		*/
		//float threshold = 65.0f;
		float threshold = 30.0f;
		float multiplier = 10000.0f;
		if ((System.Math.Abs (calculateVectors [0, 3] - calculateVectors [0, 2]) * multiplier > threshold || System.Math.Abs (calculateVectors [1, 3] - calculateVectors [1, 2]) * multiplier > threshold || System.Math.Abs (calculateVectors [2, 3] - calculateVectors [2, 2]) * multiplier > threshold || System.Math.Abs (calculateVectors [3, 3] - calculateVectors [3, 2]) * multiplier > threshold || System.Math.Abs (calculateVectors [4, 3] - calculateVectors [4, 2]) * multiplier > threshold || System.Math.Abs (calculateVectors [5, 3] - calculateVectors [5, 2]) * multiplier > threshold) && frame<200) {
			saveBody [frame, 0] = temp [0];
			for (int i = 1; i < 21; i++) {
				saveBody [frame, i] = temp [i];// * 100000.0f;
				if (centraflexureValue < System.Math.Abs (saveBody [frame, i] - saveBody [frame, i-1])) {
					centraflexureValue = System.Math.Abs (saveBody [frame, i] - saveBody [frame, i-1]);
					centraflexurePoint = frame;
				}
			}
			//Debug.Log ("R::You Moved!" + System.Math.Abs (calculateVectors [0, 3] - calculateVectors [0, 2]) * 1000 + "::::" + System.Math.Abs (calculateVectors [1, 3] - calculateVectors [1, 2]) * 1000 + "::::" + System.Math.Abs (calculateVectors [2, 3] - calculateVectors [2, 2]) * 1000);
			//Debug.Log ("L::You Moved!" + System.Math.Abs (calculateVectors [3, 3] - calculateVectors [3, 2]) * 1000 + "::::" + System.Math.Abs (calculateVectors [4, 3] - calculateVectors [4, 2]) * 1000 + "::::" + System.Math.Abs (calculateVectors [5, 3] - calculateVectors [5, 2]) * 1000);
			frame++;
		} else {
			if (frame > 9 && centraflexurePoint > 1 && centraflexurePoint < frame - 2) {
				frame = frame - 1;
				//centraflexurePoint = centraflexurePoint - 1;
				//string strPath = "5Frame_TEST.csv";
				//FileStream fs = new FileStream (strPath, FileMode.Append);
				//StreamWriter sr = new StreamWriter (fs);
				//Debug.Log ("변곡점::" + centraflexurePoint + "::::총 프레임::::" + frame);
				int i = 0;
				frameBuffer = frameBuffer + saveBody [i, 0] + "," + saveBody [i, 1] + "," + saveBody [i, 2] + "," + saveBody [i, 3] + "," + saveBody [i, 4] + "," + saveBody [i, 5] + "," + saveBody [i, 6] + "," + saveBody [i, 7] + "," + saveBody [i, 8] + "," + saveBody [i, 9] + "," + saveBody [i, 10] + "," + saveBody [i, 11] + "," + saveBody [i, 12] + "," + saveBody [i, 13] + "," + saveBody [i, 14] + "," + saveBody [i, 15] + "," + saveBody [i, 16] + "," + saveBody [i, 17] + "," + saveBody [i, 18] + "," + saveBody [i, 19] + "," + saveBody [i, 20]+",";
				//sr.WriteLine ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", saveBody [i, 0], saveBody [i, 1], saveBody [i, 2], saveBody [i, 3], saveBody [i, 4], saveBody [i, 5], saveBody [i, 6], saveBody [i, 7], saveBody [i, 8], saveBody [i, 9], saveBody [i, 10], saveBody [i, 11], saveBody [i, 12], saveBody [i, 13], saveBody [i, 14], saveBody [i, 15], saveBody [i, 16], saveBody [i, 17], saveBody [i, 18], saveBody [i, 19], saveBody [i, 20]);
				//i = -1;
				frameBuffer = frameBuffer + saveBody [(i + centraflexurePoint) / 2, 0] + "," + saveBody [(i + centraflexurePoint) / 2, 1] + "," + saveBody [(i + centraflexurePoint) / 2, 2] + "," + saveBody [(i + centraflexurePoint) / 2, 3] + "," + saveBody [(i + centraflexurePoint) / 2, 4] + "," + saveBody [(i + centraflexurePoint) / 2, 5] + "," + saveBody [(i + centraflexurePoint) / 2, 6] + "," + saveBody [(i + centraflexurePoint) / 2, 7] + "," + saveBody [(i + centraflexurePoint) / 2, 8] + "," + saveBody [(i + centraflexurePoint) / 2, 9] + "," + saveBody [(i + centraflexurePoint) / 2, 10] + "," + saveBody [(i + centraflexurePoint) / 2, 11] + "," + saveBody [(i + centraflexurePoint) / 2, 12] + "," + saveBody [(i + centraflexurePoint) / 2, 13] + "," + saveBody [(i + centraflexurePoint) / 2, 14] + "," + saveBody [(i + centraflexurePoint) / 2, 15] + "," + saveBody [(i + centraflexurePoint) / 2, 16] + "," + saveBody [(i + centraflexurePoint) / 2, 17] + "," + saveBody [(i + centraflexurePoint) / 2, 18] + "," + saveBody [(i + centraflexurePoint) / 2, 19] + "," + saveBody [(i + centraflexurePoint) / 2, 20]+",";
				//sr.WriteLine ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", saveBody [(i + centraflexurePoint) / 2, 0], saveBody [(i + centraflexurePoint) / 2, 1], saveBody [(i + centraflexurePoint) / 2, 2], saveBody [(i + centraflexurePoint) / 2, 3], saveBody [(i + centraflexurePoint) / 2, 4], saveBody [(i + centraflexurePoint) / 2, 5], saveBody [(i + centraflexurePoint) / 2, 6], saveBody [(i + centraflexurePoint) / 2, 7], saveBody [(i + centraflexurePoint) / 2, 8], saveBody [(i + centraflexurePoint) / 2, 9], saveBody [(i + centraflexurePoint) / 2, 10], saveBody [(i + centraflexurePoint) / 2, 11], saveBody [(i + centraflexurePoint) / 2, 12], saveBody [(i + centraflexurePoint) / 2, 13], saveBody [(i + centraflexurePoint) / 2, 14], saveBody [(i + centraflexurePoint) / 2, 15], saveBody [(i + centraflexurePoint) / 2, 16], saveBody [(i + centraflexurePoint) / 2, 17], saveBody [(i + centraflexurePoint) / 2, 18], saveBody [(i + centraflexurePoint) / 2, 19], saveBody [(i + centraflexurePoint) / 2, 20]);
				i = centraflexurePoint;
				frameBuffer = frameBuffer + saveBody [i, 0] + "," + saveBody [i, 1] + "," + saveBody [i, 2] + "," + saveBody [i, 3] + "," + saveBody [i, 4] + "," + saveBody [i, 5] + "," + saveBody [i, 6] + "," + saveBody [i, 7] + "," + saveBody [i, 8] + "," + saveBody [i, 9] + "," + saveBody [i, 10] + "," + saveBody [i, 11] + "," + saveBody [i, 12] + "," + saveBody [i, 13] + "," + saveBody [i, 14] + "," + saveBody [i, 15] + "," + saveBody [i, 16] + "," + saveBody [i, 17] + "," + saveBody [i, 18] + "," + saveBody [i, 19] + "," + saveBody [i, 20]+",";
				//sr.WriteLine ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", saveBody [i, 0], saveBody [i, 1], saveBody [i, 2], saveBody [i, 3], saveBody [i, 4], saveBody [i, 5], saveBody [i, 6], saveBody [i, 7], saveBody [i, 8], saveBody [i, 9], saveBody [i, 10], saveBody [i, 11], saveBody [i, 12], saveBody [i, 13], saveBody [i, 14], saveBody [i, 15], saveBody [i, 16], saveBody [i, 17], saveBody [i, 18], saveBody [i, 19], saveBody [i, 20]);
				//i = centraflexurePoint-1;
				frameBuffer = frameBuffer + saveBody [(centraflexurePoint+frame) / 2, 0] + "," + saveBody [(centraflexurePoint+frame) / 2, 1] + "," + saveBody [(centraflexurePoint+frame) / 2, 2] + "," + saveBody [(centraflexurePoint+frame) / 2, 3] + "," + saveBody [(centraflexurePoint+frame) / 2, 4] + "," + saveBody [(centraflexurePoint+frame) / 2, 5] + "," + saveBody [(centraflexurePoint+frame) / 2, 6] + "," + saveBody [(centraflexurePoint+frame) / 2, 7] + "," + saveBody [(centraflexurePoint+frame) / 2, 8] + "," + saveBody [(centraflexurePoint+frame) / 2, 9] + "," + saveBody [(centraflexurePoint+frame) / 2, 10] + "," + saveBody [(centraflexurePoint+frame) / 2, 11] + "," + saveBody [(centraflexurePoint+frame) / 2, 12] + "," + saveBody [(centraflexurePoint+frame) / 2, 13] + "," + saveBody [(centraflexurePoint+frame) / 2, 14] + "," + saveBody [(centraflexurePoint+frame) / 2, 15] + "," + saveBody [(centraflexurePoint+frame) / 2, 16] + "," + saveBody [(centraflexurePoint+frame) / 2, 17] + "," + saveBody [(centraflexurePoint+frame) / 2, 18] + "," + saveBody [(centraflexurePoint+frame) / 2, 19] + "," + saveBody [(centraflexurePoint+frame) / 2, 20]+",";
				//sr.WriteLine ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", saveBody [(centraflexurePoint + frame) / 2, 0], saveBody [(centraflexurePoint + frame) / 2, 1], saveBody [(centraflexurePoint + frame) / 2, 2], saveBody [(centraflexurePoint + frame) / 2, 3], saveBody [(centraflexurePoint + frame) / 2, 4], saveBody [(centraflexurePoint + frame) / 2, 5], saveBody [(centraflexurePoint + frame) / 2, 6], saveBody [(centraflexurePoint + frame) / 2, 7], saveBody [(centraflexurePoint + frame) / 2, 8], saveBody [(centraflexurePoint + frame) / 2, 9], saveBody [(centraflexurePoint + frame) / 2, 10], saveBody [(centraflexurePoint + frame) / 2, 11], saveBody [(centraflexurePoint + frame) / 2, 12], saveBody [(centraflexurePoint + frame) / 2, 13], saveBody [(centraflexurePoint + frame) / 2, 14], saveBody [(centraflexurePoint + frame) / 2, 15], saveBody [(centraflexurePoint + frame) / 2, 16], saveBody [(centraflexurePoint + frame) / 2, 17], saveBody [(centraflexurePoint + frame) / 2, 18], saveBody [(centraflexurePoint + frame) / 2, 19], saveBody [(centraflexurePoint + frame) / 2, 20]);
				i = frame;
				frameBuffer = frameBuffer + saveBody [i, 0] + "," + saveBody [i, 1] + "," + saveBody [i, 2] + "," + saveBody [i, 3] + "," + saveBody [i, 4] + "," + saveBody [i, 5] + "," + saveBody [i, 6] + "," + saveBody [i, 7] + "," + saveBody [i, 8] + "," + saveBody [i, 9] + "," + saveBody [i, 10] + "," + saveBody [i, 11] + "," + saveBody [i, 12] + "," + saveBody [i, 13] + "," + saveBody [i, 14] + "," + saveBody [i, 15] + "," + saveBody [i, 16] + "," + saveBody [i, 17] + "," + saveBody [i, 18] + "," + saveBody [i, 19] + "," + saveBody [i, 20]+",";
				//sr.WriteLine ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}", saveBody [i, 0], saveBody [i, 1], saveBody [i, 2], saveBody [i, 3], saveBody [i, 4], saveBody [i, 5], saveBody [i, 6], saveBody [i, 7], saveBody [i, 8], saveBody [i, 9], saveBody [i, 10], saveBody [i, 11], saveBody [i, 12], saveBody [i, 13], saveBody [i, 14], saveBody [i, 15], saveBody [i, 16], saveBody [i, 17], saveBody [i, 18], saveBody [i, 19], saveBody [i, 20]);
				//sr.WriteLine ("0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
				//sr.Close ();
				//fs.Close ();

				//frame = 5;
				frameBuffer = frameBuffer.Substring (0, frameBuffer.Length - 1);
				Debug.Log (frameBuffer);
				Server.frame = 5;
				Server.fdata = frameBuffer;
				frame = 0;
				frameBuffer = "";
			} else {
				/*
				if (frame > 9) {
					Debug.Log ("::::총 프레임::::" + frame);
				}*/
			}
			centraflexureValue = -1.0f;
			centraflexurePoint = 0;
			Debug.Log ("Centraflexure reset");
		}

		beforeBody = temp;
		calculatingTrigger = false;
	}

	//public static Camera camera;
	// estimate the next state and completeness of the gesture
	public static void CheckForGesture(uint userId, ref GestureData gestureData, float timestamp, ref Vector3[] jointsPos, ref bool[] jointsTracked)
	{
		//Debug.Log("checked");
		if (!calculatingTrigger) {
			timeCountTrigger++;
		}

		rightWristVector = jointsPos [rightWristIndex];

		if (initialXTrigger) {
			XMinimum = System.Math.Abs(jointsPos [leftWristIndex].x);//-(jointsPos[leftElbowIndex].x-jointsPos [leftWristIndex].x)/6;
			XMaximum = jointsPos [rightWristIndex].x;//+(jointsPos [rightWristIndex].x-jointsPos[rightElbowIndex].x)/6;
			KinectManager.XMinimum = XMinimum;
			KinectManager.XMaximum = XMaximum;
			initialXTrigger = false;
		}
		if (initialYTrigger) {
			YMinimum = System.Math.Abs(jointsPos [leftWristIndex].y);//-(jointsPos[leftElbowIndex].y-jointsPos [leftWristIndex].y)/6;
			YMaximum = jointsPos [rightWristIndex].y;//+(jointsPos [rightWristIndex].y-jointsPos[rightElbowIndex].y)/6;
			KinectManager.YMinimum = YMinimum;
			KinectManager.YMaximum = YMaximum;
			initialYTrigger = false;
		}
		if (initialZTrigger) {
			ZMinimum = jointsPos [rightWristIndex].z;//-(jointsPos[rightWristIndex].z-jointsPos [rightElbowIndex].z)/6;
			ZMaximum = ZMinimum+2*(jointsPos [rightShoulderIndex].z-jointsPos [rightWristIndex].z);//+(jointsPos [leftWristIndex].z-jointsPos[leftElbowIndex].z)/6;
			KinectManager.ZMinimum = ZMinimum;
			KinectManager.ZMaximum = ZMaximum;
			initialZTrigger = false;
		}
			
		if (AuXTrigger) {
			XMinimum = System.Math.Abs(jointsPos [leftWristIndex].x);//-(jointsPos[leftElbowIndex].x-jointsPos [leftWristIndex].x)/6;
			XMaximum = jointsPos [rightWristIndex].x;//+(jointsPos [rightWristIndex].x-jointsPos[rightElbowIndex].x)/6;
			KinectManager.AuXMinimum = XMinimum;
			KinectManager.AuXMaximum = XMaximum;
			AuXTrigger = false;
		}
		if (AuYTrigger) {
			YMinimum = System.Math.Abs(jointsPos [leftWristIndex].y);//-(jointsPos[leftElbowIndex].y-jointsPos [leftWristIndex].y)/6;
			YMaximum = jointsPos [rightWristIndex].y;//+(jointsPos [rightWristIndex].y-jointsPos[rightElbowIndex].y)/6;
			KinectManager.AuYMinimum = YMinimum;
			KinectManager.AuYMaximum = YMaximum;
			AuYTrigger = false;
		}
		if (AuZTrigger) {
			ZMinimum = jointsPos [rightWristIndex].z;//-(jointsPos[rightWristIndex].z-jointsPos [rightElbowIndex].z)/6;
			ZMaximum = ZMinimum+2*(jointsPos [rightShoulderIndex].z-jointsPos [rightWristIndex].z);//+(jointsPos [leftWristIndex].z-jointsPos[leftElbowIndex].z)/6;
			KinectManager.AuZMinimum = ZMinimum;
			KinectManager.AuZMaximum = ZMaximum;
			AuZTrigger = false;
		}




		if (initialBodyTrigger) {
			beforeBody = new float[] {
				(jointsPos [rightWristIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightWristIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightWristIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [rightElbowIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightElbowIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightElbowIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [rightShoulderIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightShoulderIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightShoulderIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [shoulderCenterIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [shoulderCenterIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [shoulderCenterIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftWristIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftWristIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftWristIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftElbowIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftElbowIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftElbowIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftShoulderIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftShoulderIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftShoulderIndex].z+ZMinimum)/(ZMaximum+ZMinimum)};
			initialBodyTrigger = false;
		}

		//UserInputs of codes down there, is just a bool reference which act as Trigger of Checking User Input

		//Debug.Log (timeCountTrigger + "::::" + readyTrigger + "::::" + calculatingTrigger + "::::" + Server.serverReady);


		//30 data in one seco
		if (!KinectManager.AuToken && timeCountTrigger > 2 && readyTrigger==true && calculatingTrigger==false && Server.serverReady==false) { // && !UserInput ) {
			//Debug.Log ("Gesture calibration called");
			tempBody = new float[] {
				(jointsPos [rightWristIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightWristIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightWristIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [rightElbowIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightElbowIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightElbowIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [rightShoulderIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [rightShoulderIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [rightShoulderIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [shoulderCenterIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [shoulderCenterIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [shoulderCenterIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftWristIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftWristIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftWristIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftElbowIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftElbowIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftElbowIndex].z+ZMinimum)/(ZMaximum+ZMinimum),
				(jointsPos [leftShoulderIndex].x+XMinimum)/(XMaximum+XMinimum),
				(jointsPos [leftShoulderIndex].y+YMinimum)/(YMaximum+YMinimum),
				(jointsPos [leftShoulderIndex].z+ZMinimum)/(ZMaximum+ZMinimum)};
			calculateData (tempBody,timestamp);
			timeCountTrigger = 0;
		}

		/*
		if(UserInput){
		  SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
			SetScreenPos(userId, ref gestureData, ref jointsPos, ref jointsTracked);
			=gestureData.screenPos.x;
			=gestureData.screenPos.y;
		}
		*/

		if (gestureData.complete)
			return;


		float bandSize = (jointsPos[shoulderCenterIndex].y - jointsPos[hipCenterIndex].y);
		float gestureTop = jointsPos[shoulderCenterIndex].y + bandSize / 2;
		float gestureBottom = jointsPos[shoulderCenterIndex].y - bandSize;
		float gestureRight = jointsPos[rightHipIndex].x;
		float gestureLeft = jointsPos[leftHipIndex].x;

		switch(gestureData.gesture)
		{
		// check for RaiseRightHand
		case Gestures.RaiseRightHand:
			switch(gestureData.state)
			{
			case 0:  // gesture detection
				if(jointsTracked[rightHandIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
				}
				break;

			case 1:  // gesture complete
				bool isInPose = jointsTracked[rightHandIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.1f;

				Vector3 jointPos = jointsPos[gestureData.joint];
				CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.PoseCompleteDuration);
				break;
			}
			break;

			// check for RaiseLeftHand
		case Gestures.RaiseLeftHand:
			switch(gestureData.state)
			{
			case 0:  // gesture detection
				if(jointsTracked[leftHandIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
				}
				break;

			case 1:  // gesture complete
				bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.1f;

				Vector3 jointPos = jointsPos[gestureData.joint];
				CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.PoseCompleteDuration);
				break;
			}
			break;

			// check for Psi
		case Gestures.Psi:
			switch(gestureData.state)
			{
			case 0:  // gesture detection
				if(jointsTracked[rightHandIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.1f &&
					jointsTracked[leftHandIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
				}
				break;

			case 1:  // gesture complete
				bool isInPose = jointsTracked[rightHandIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.1f &&
					jointsTracked[leftHandIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.1f;

				Vector3 jointPos = jointsPos[gestureData.joint];
				CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.PoseCompleteDuration);
				break;
			}
			break;

			// check for Tpose
		case Gestures.Tpose:
			switch(gestureData.state)
			{
			case 0:  // gesture detection
				if(jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[rightShoulderIndex] &&
					Mathf.Abs(jointsPos[rightElbowIndex].y - jointsPos[rightShoulderIndex].y) < 0.1f &&  // 0.07f
					Mathf.Abs(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) < 0.1f &&  // 0.7f
					jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[leftShoulderIndex] &&
					Mathf.Abs(jointsPos[leftElbowIndex].y - jointsPos[leftShoulderIndex].y) < 0.1f &&
					Mathf.Abs(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) < 0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
				}
				break;

			case 1:  // gesture complete
				bool isInPose = jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[rightShoulderIndex] &&
					Mathf.Abs(jointsPos[rightElbowIndex].y - jointsPos[rightShoulderIndex].y) < 0.1f &&  // 0.7f
					Mathf.Abs(jointsPos[rightHandIndex].y - jointsPos[rightShoulderIndex].y) < 0.1f &&  // 0.7f
					jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[leftShoulderIndex] &&
					Mathf.Abs(jointsPos[leftElbowIndex].y - jointsPos[leftShoulderIndex].y) < 0.1f &&
					Mathf.Abs(jointsPos[leftHandIndex].y - jointsPos[leftShoulderIndex].y) < 0.1f;

				Vector3 jointPos = jointsPos[gestureData.joint];
				CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.PoseCompleteDuration);

				break;
			}
			break;

			// check for Stop
		case Gestures.Stop:
			switch(gestureData.state)
			{
			case 0:  // gesture detection
				if(jointsTracked[rightHandIndex] && jointsTracked[rightHipIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightHipIndex].y) < 0.1f &&
					(jointsPos[rightHandIndex].x - jointsPos[rightHipIndex].x) >= 0.4f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[leftHipIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftHipIndex].y) < 0.1f &&
					(jointsPos[leftHandIndex].x - jointsPos[leftHipIndex].x) <= -0.4f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
				}
				break;

			case 1:  // gesture complete
				bool isInPose = (gestureData.joint == rightHandIndex) ?
					(jointsTracked[rightHandIndex] && jointsTracked[rightHipIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[rightHipIndex].y) < 0.1f &&
						(jointsPos[rightHandIndex].x - jointsPos[rightHipIndex].x) >= 0.4f) :
					(jointsTracked[leftHandIndex] && jointsTracked[leftHipIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[leftHipIndex].y) < 0.1f &&
						(jointsPos[leftHandIndex].x - jointsPos[leftHipIndex].x) <= -0.4f);

				Vector3 jointPos = jointsPos[gestureData.joint];
				CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.PoseCompleteDuration);
				break;

			}
			break;

			// check for Wave
		case Gestures.Wave:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > 0.1f &&
					(jointsPos[rightHandIndex].x - jointsPos[rightElbowIndex].x) > 0.05f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.3f;
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > 0.1f &&
					(jointsPos[leftHandIndex].x - jointsPos[leftElbowIndex].x) < -0.05f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.3f;
				}
				break;

			case 1:  // gesture - phase 2
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > 0.1f && 
						(jointsPos[rightHandIndex].x - jointsPos[rightElbowIndex].x) < -0.05f :
						jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > 0.1f &&
						(jointsPos[leftHandIndex].x - jointsPos[leftElbowIndex].x) > 0.05f;

					if(isInPose)
					{
						gestureData.timestamp = timestamp;
						gestureData.state++;
						gestureData.progress = 0.7f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;

			case 2:  // gesture phase 3 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > 0.1f && 
						(jointsPos[rightHandIndex].x - jointsPos[rightElbowIndex].x) > 0.05f :
						jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > 0.1f &&
						(jointsPos[leftHandIndex].x - jointsPos[leftElbowIndex].x) < -0.05f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for Click
		case Gestures.Click:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.3f;

					// set screen position at the start, because this is the most accurate click position
					SetScreenPos(userId, ref gestureData, ref jointsPos, ref jointsTracked);
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.3f;

					// set screen position at the start, because this is the most accurate click position
					SetScreenPos(userId, ref gestureData, ref jointsPos, ref jointsTracked);
				}
				break;

			case 1:  // gesture - phase 2
				//						if((timestamp - gestureData.timestamp) < 1.0f)
				//						{
				//							bool isInPose = gestureData.joint == rightHandIndex ?
				//								jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
				//								//(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f && 
				//								Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) < 0.08f &&
				//								(jointsPos[rightHandIndex].z - gestureData.jointPos.z) < -0.05f :
				//								jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
				//								//(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
				//								Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) < 0.08f &&
				//								(jointsPos[leftHandIndex].z - gestureData.jointPos.z) < -0.05f;
				//				
				//							if(isInPose)
				//							{
				//								gestureData.timestamp = timestamp;
				//								gestureData.jointPos = jointsPos[gestureData.joint];
				//								gestureData.state++;
				//								gestureData.progress = 0.7f;
				//							}
				//							else
				//							{
				//								// check for stay-in-place
				//								Vector3 distVector = jointsPos[gestureData.joint] - gestureData.jointPos;
				//								isInPose = distVector.magnitude < 0.05f;
				//
				//								Vector3 jointPos = jointsPos[gestureData.joint];
				//								CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, Constants.ClickStayDuration);
				//							}
				//						}
				//						else
				{
					// check for stay-in-place
					Vector3 distVector = jointsPos[gestureData.joint] - gestureData.jointPos;
					bool isInPose = distVector.magnitude < 0.05f;

					Vector3 jointPos = jointsPos[gestureData.joint];
					CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, KinectWrapper.Constants.ClickStayDuration);
					//							SetGestureCancelled(gestureData);
				}
				break;

				//					case 2:  // gesture phase 3 = complete
				//						if((timestamp - gestureData.timestamp) < 1.0f)
				//						{
				//							bool isInPose = gestureData.joint == rightHandIndex ?
				//								jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
				//								//(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f && 
				//								Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) < 0.08f &&
				//								(jointsPos[rightHandIndex].z - gestureData.jointPos.z) > 0.05f :
				//								jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
				//								//(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
				//								Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) < 0.08f &&
				//								(jointsPos[leftHandIndex].z - gestureData.jointPos.z) > 0.05f;
				//
				//							if(isInPose)
				//							{
				//								Vector3 jointPos = jointsPos[gestureData.joint];
				//								CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
				//							}
				//						}
				//						else
				//						{
				//							// cancel the gesture
				//							SetGestureCancelled(ref gestureData);
				//						}
				//						break;
			}
			break;

			// check for SwipeLeft
		case Gestures.SwipeLeft:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				//						if(jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
				//					       (jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) > -0.05f &&
				//					       (jointsPos[rightHandIndex].x - jointsPos[rightElbowIndex].x) > 0f)
				//						{
				//							SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
				//							gestureData.progress = 0.5f;
				//						}

				if(jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
					jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
					jointsPos[rightHandIndex].x <= gestureRight && jointsPos[rightHandIndex].x > gestureLeft)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.1f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					//							bool isInPose = jointsTracked[rightHandIndex] && jointsTracked[rightElbowIndex] &&
					//								Mathf.Abs(jointsPos[rightHandIndex].y - jointsPos[rightElbowIndex].y) < 0.1f && 
					//								Mathf.Abs(jointsPos[rightHandIndex].y - gestureData.jointPos.y) < 0.08f && 
					//								(jointsPos[rightHandIndex].x - gestureData.jointPos.x) < -0.15f;

					bool isInPose = jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
						jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
						jointsPos[rightHandIndex].x < gestureLeft;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
					else if(jointsPos[rightHandIndex].x <= gestureRight)
					{
						float gestureSize = gestureRight - gestureLeft;
						gestureData.progress = gestureSize > 0.01f ? (gestureRight - jointsPos[rightHandIndex].x) / gestureSize : 0f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for SwipeRight
		case Gestures.SwipeRight:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				//						if(jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
				//					            (jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) > -0.05f &&
				//					            (jointsPos[leftHandIndex].x - jointsPos[leftElbowIndex].x) < 0f)
				//						{
				//							SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
				//							gestureData.progress = 0.5f;
				//						}

				if(jointsTracked[leftHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
					jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
					jointsPos[leftHandIndex].x >= gestureLeft && jointsPos[leftHandIndex].x < gestureRight)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.1f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					//							bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[leftElbowIndex] &&
					//								Mathf.Abs(jointsPos[leftHandIndex].y - jointsPos[leftElbowIndex].y) < 0.1f &&
					//								Mathf.Abs(jointsPos[leftHandIndex].y - gestureData.jointPos.y) < 0.08f && 
					//								(jointsPos[leftHandIndex].x - gestureData.jointPos.x) > 0.15f;

					bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
						jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
						jointsPos[leftHandIndex].x > gestureRight;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
					else if(jointsPos[leftHandIndex].x >= gestureLeft)
					{
						float gestureSize = gestureRight - gestureLeft;
						gestureData.progress = gestureSize > 0.01f ? (jointsPos[leftHandIndex].x - gestureLeft) / gestureSize : 0f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for SwipeUp
		case Gestures.SwipeUp:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) < 0.0f &&
					(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) > -0.15f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.5f;
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) < 0.0f &&
					(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) > -0.15f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[leftShoulderIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.05f && 
						Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) <= 0.1f :
						jointsTracked[leftHandIndex] && jointsTracked[rightShoulderIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.05f && 
						Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) <= 0.1f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for SwipeDown
		case Gestures.SwipeDown:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[leftShoulderIndex].y) > 0.05f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.5f;
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[rightShoulderIndex].y) > 0.05f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) < -0.15f && 
						Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) <= 0.1f :
						jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) < -0.15f &&
						Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) <= 0.1f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for RightHandCursor
		case Gestures.RightHandCursor:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1 (perpetual)
				if(jointsTracked[rightHandIndex] && jointsTracked[rightHipIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[rightHipIndex].y) > -0.1f)
				{
					gestureData.joint = rightHandIndex;
					gestureData.timestamp = timestamp;
					//gestureData.jointPos = jointsPos[rightHandIndex];
					SetScreenPos(userId, ref gestureData, ref jointsPos, ref jointsTracked);
					gestureData.progress = 0.7f;
				}
				else
				{
					// cancel the gesture
					//SetGestureCancelled(ref gestureData);
					gestureData.progress = 0f;
				}
				break;

			}
			break;

			// check for LeftHandCursor
		case Gestures.LeftHandCursor:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1 (perpetual)
				if(jointsTracked[leftHandIndex] && jointsTracked[leftHipIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[leftHipIndex].y) > -0.1f)
				{
					gestureData.joint = leftHandIndex;
					gestureData.timestamp = timestamp;
					//gestureData.jointPos = jointsPos[leftHandIndex];
					SetScreenPos(userId, ref gestureData, ref jointsPos, ref jointsTracked);
					gestureData.progress = 0.7f;
				}
				else
				{
					// cancel the gesture
					//SetGestureCancelled(ref gestureData);
					gestureData.progress = 0f;
				}
				break;

			}
			break;

			// check for ZoomOut
		case Gestures.ZoomOut:
			Vector3 vectorZoomOut = (Vector3)jointsPos[rightHandIndex] - jointsPos[leftHandIndex];
			float distZoomOut = vectorZoomOut.magnitude;

			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
					jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
					jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
					distZoomOut < 0.3f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.tagVector = Vector3.right;
					gestureData.tagFloat = 0f;
					gestureData.progress = 0.3f;
				}
				break;

			case 1:  // gesture phase 2 = zooming
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					float angleZoomOut = Vector3.Angle(gestureData.tagVector, vectorZoomOut) * Mathf.Sign(vectorZoomOut.y - gestureData.tagVector.y);
					bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
						jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
						jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
						distZoomOut < 1.5f && Mathf.Abs(angleZoomOut) < 20f;

					if(isInPose)
					{
						SetZoomFactor(userId, ref gestureData, 1.0f, ref jointsPos, ref jointsTracked);
						gestureData.timestamp = timestamp;
						gestureData.progress = 0.7f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;

			}
			break;

			// check for ZoomIn
		case Gestures.ZoomIn:
			Vector3 vectorZoomIn = (Vector3)jointsPos[rightHandIndex] - jointsPos[leftHandIndex];
			float distZoomIn = vectorZoomIn.magnitude;

			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
					jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
					jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
					distZoomIn >= 0.7f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.tagVector = Vector3.right;
					gestureData.tagFloat = distZoomIn;
					gestureData.progress = 0.3f;
				}
				break;

			case 1:  // gesture phase 2 = zooming
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					float angleZoomIn = Vector3.Angle(gestureData.tagVector, vectorZoomIn) * Mathf.Sign(vectorZoomIn.y - gestureData.tagVector.y);
					bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
						jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
						jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
						distZoomIn >= 0.2f && Mathf.Abs(angleZoomIn) < 20f;

					if(isInPose)
					{
						SetZoomFactor(userId, ref gestureData, 0.0f, ref jointsPos, ref jointsTracked);
						gestureData.timestamp = timestamp;
						gestureData.progress = 0.7f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;

			}
			break;

			// check for Wheel
		case Gestures.Wheel:
			Vector3 vectorWheel = (Vector3)jointsPos[rightHandIndex] - jointsPos[leftHandIndex];
			float distWheel = vectorWheel.magnitude;

			//				Debug.Log(string.Format("{0}. Dist: {1:F1}, Tag: {2:F1}, Diff: {3:F1}", gestureData.state,
			//				                        distWheel, gestureData.tagFloat, Mathf.Abs(distWheel - gestureData.tagFloat)));

			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
					jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
					jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
					distWheel >= 0.3f && distWheel < 0.7f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.tagVector = Vector3.right;
					gestureData.tagFloat = distWheel;
					gestureData.progress = 0.3f;
				}
				break;

			case 1:  // gesture phase 2 = turning wheel
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					float angle = Vector3.Angle(gestureData.tagVector, vectorWheel) * Mathf.Sign(vectorWheel.y - gestureData.tagVector.y);
					bool isInPose = jointsTracked[leftHandIndex] && jointsTracked[rightHandIndex] && jointsTracked[hipCenterIndex] && jointsTracked[shoulderCenterIndex] && jointsTracked[leftHipIndex] && jointsTracked[rightHipIndex] &&
						jointsPos[leftHandIndex].y >= gestureBottom && jointsPos[leftHandIndex].y <= gestureTop &&
						jointsPos[rightHandIndex].y >= gestureBottom && jointsPos[rightHandIndex].y <= gestureTop &&
						distWheel >= 0.3f && distWheel < 0.7f && 
						Mathf.Abs(distWheel - gestureData.tagFloat) < 0.1f;

					if(isInPose)
					{
						//SetWheelRotation(userId, ref gestureData, gestureData.tagVector, vectorWheel);
						gestureData.screenPos.z = angle;  // wheel angle
						gestureData.timestamp = timestamp;
						gestureData.tagFloat = distWheel;
						gestureData.progress = 0.7f;
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;

			}
			break;

			// check for Jump
		case Gestures.Jump:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[hipCenterIndex] && 
					(jointsPos[hipCenterIndex].y > 0.9f) && (jointsPos[hipCenterIndex].y < 1.3f))
				{
					SetGestureJoint(ref gestureData, timestamp, hipCenterIndex, jointsPos[hipCenterIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = jointsTracked[hipCenterIndex] &&
						(jointsPos[hipCenterIndex].y - gestureData.jointPos.y) > 0.15f && 
						Mathf.Abs(jointsPos[hipCenterIndex].x - gestureData.jointPos.x) < 0.2f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for Squat
		case Gestures.Squat:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[hipCenterIndex] && 
					(jointsPos[hipCenterIndex].y <= 0.9f))
				{
					SetGestureJoint(ref gestureData, timestamp, hipCenterIndex, jointsPos[hipCenterIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = jointsTracked[hipCenterIndex] &&
						(jointsPos[hipCenterIndex].y - gestureData.jointPos.y) < -0.15f && 
						Mathf.Abs(jointsPos[hipCenterIndex].x - gestureData.jointPos.x) < 0.2f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for Push
		case Gestures.Push:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
					Mathf.Abs(jointsPos[rightHandIndex].x - jointsPos[rightShoulderIndex].x) < 0.2f &&
					(jointsPos[rightHandIndex].z - jointsPos[leftElbowIndex].z) < -0.2f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.5f;
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f &&
					Mathf.Abs(jointsPos[leftHandIndex].x - jointsPos[leftShoulderIndex].x) < 0.2f &&
					(jointsPos[leftHandIndex].z - jointsPos[rightElbowIndex].z) < -0.2f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[rightShoulderIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
						Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) < 0.2f &&
						(jointsPos[rightHandIndex].z - gestureData.jointPos.z) < -0.1f :
						jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[leftShoulderIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f &&
						Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) < 0.2f &&
						(jointsPos[leftHandIndex].z - gestureData.jointPos.z) < -0.1f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// check for Pull
		case Gestures.Pull:
			switch(gestureData.state)
			{
			case 0:  // gesture detection - phase 1
				if(jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[rightShoulderIndex] &&
					(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
					Mathf.Abs(jointsPos[rightHandIndex].x - jointsPos[rightShoulderIndex].x) < 0.2f &&
					(jointsPos[rightHandIndex].z - jointsPos[leftElbowIndex].z) < -0.3f)
				{
					SetGestureJoint(ref gestureData, timestamp, rightHandIndex, jointsPos[rightHandIndex]);
					gestureData.progress = 0.5f;
				}
				else if(jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[leftShoulderIndex] &&
					(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f &&
					Mathf.Abs(jointsPos[leftHandIndex].x - jointsPos[leftShoulderIndex].x) < 0.2f &&
					(jointsPos[leftHandIndex].z - jointsPos[rightElbowIndex].z) < -0.3f)
				{
					SetGestureJoint(ref gestureData, timestamp, leftHandIndex, jointsPos[leftHandIndex]);
					gestureData.progress = 0.5f;
				}
				break;

			case 1:  // gesture phase 2 = complete
				if((timestamp - gestureData.timestamp) < 1.5f)
				{
					bool isInPose = gestureData.joint == rightHandIndex ?
						jointsTracked[rightHandIndex] && jointsTracked[leftElbowIndex] && jointsTracked[rightShoulderIndex] &&
						(jointsPos[rightHandIndex].y - jointsPos[leftElbowIndex].y) > -0.1f &&
						Mathf.Abs(jointsPos[rightHandIndex].x - gestureData.jointPos.x) < 0.2f &&
						(jointsPos[rightHandIndex].z - gestureData.jointPos.z) > 0.1f :
						jointsTracked[leftHandIndex] && jointsTracked[rightElbowIndex] && jointsTracked[leftShoulderIndex] &&
						(jointsPos[leftHandIndex].y - jointsPos[rightElbowIndex].y) > -0.1f &&
						Mathf.Abs(jointsPos[leftHandIndex].x - gestureData.jointPos.x) < 0.2f &&
						(jointsPos[leftHandIndex].z - gestureData.jointPos.z) > 0.1f;

					if(isInPose)
					{
						Vector3 jointPos = jointsPos[gestureData.joint];
						CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
					}
				}
				else
				{
					// cancel the gesture
					SetGestureCancelled(ref gestureData);
				}
				break;
			}
			break;

			// here come more gesture-cases
		}

	}

}
