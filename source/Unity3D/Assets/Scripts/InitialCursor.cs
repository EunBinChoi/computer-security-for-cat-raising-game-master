using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitialCursor : MonoBehaviour
{

    float currentTime, buttonEnterTime, poseTime = 0.0f;
    int ButtonSelector = 4;
    GameObject Timer;
    //GameObject TimerText;
    GameObject TimerText;
    public static int poseNum = 0;
    GameObject completeText, poseImage;
    public static Vector3 rightWristVector;
    GameObject posDone, cubeMan;
    private bool startTrigger;

    // Use this for initialization
    void Start()
    {
        currentTime = buttonEnterTime = 0.0f;
        Timer = GameObject.Find("Timer");
        TimerText = GameObject.Find("TimerText");
        poseImage = GameObject.Find("Image");
        posDone = GameObject.Find("PoseComplete");
        cubeMan = GameObject.Find("Cubeman");
        startTrigger = false;
        posDone.SetActive(false);
        poseImage.SetActive(true);
        poseNum = 1;
    }

    // Update is called once per frame
    void Update()
    {
        KinectManager kinectManager = KinectManager.Instance;
        if ((!kinectManager || !kinectManager.IsInitialized() || !kinectManager.IsUserDetected()))
        {
        }
        else
        {
            uint userId = kinectManager.GetPlayer1ID();
            kinectManager.DetectGesture(userId, KinectGestures.Gestures.RaiseRightHand);
        }
        currentTime = Time.time;
		//3초 이상이면 전환
		if (poseNum <= 3) {
			if (startTrigger)
			{
				startPose();
			}
			if (buttonEnterTime != 0.0f)
			{
				Timer.GetComponent<Image>().fillAmount = (currentTime - buttonEnterTime) / 2.0f;

				if (currentTime - buttonEnterTime >= 2.0f)
				{
					posDone.SetActive(false);
					if (ButtonSelector == 0) {
						PrevPose ();
						buttonEnterTime = 0.0f;
					} else if (ButtonSelector == 1) {
						NextPose ();
						buttonEnterTime = 0.0f;

					} else if (ButtonSelector == 2) {
						startTrigger = true;
						buttonEnterTime = 0.0f;

					}
					//rePose();
					else if (ButtonSelector == 3) {
						startTrigger = true;
						buttonEnterTime = 0.0f;

					}
					else if (ButtonSelector == 4) {
						startTrigger = false;
						posDone.SetActive (true);
						buttonEnterTime = 0.0f;
					}
					else {
						startTrigger = false; 
					}
				}
				else { }
			}
			else if (buttonEnterTime == 0.0f)
			{
				Timer.GetComponent<Image>().fillAmount = 0.0f;
			}
		} else if (poseNum > 3) {
			poseImage.SetActive (false);
			posDone.SetActive (true);
		}
        SetMouseFunction();
    }

    protected void SetMouseFunction()
    {
        Vector3 temp;
        rightWristVector = KinectGestures.rightWristVector;
        //Debug.Log (Camera.main.pixelWidth + ", " + Camera.main.pixelHeight);

        temp.x = Camera.main.pixelWidth / 2 + rightWristVector.x * Camera.main.pixelWidth;
        temp.y = rightWristVector.y * Camera.main.pixelHeight - Camera.main.pixelHeight;
        temp.z = 0;

        transform.position = temp;
        //.transform.position.x = Camera.main.ScreenToViewportPoint(temp).x;
        //KinectCursur.transform.position.y = Camera.main.ScreenToViewportPoint(temp).y;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name.Equals("NextPose"))
        {
            MouseEnterNextposeButton();
        }
        else if (collision.name.Equals("ReStart"))
		{
            MouseEnterRestartButton();
        }
        else if (collision.name.Equals("Start"))
		{
            MouseEnterStartButton();
        }
		else if (collision.name.Equals("PrevPose"))
		{	
			MouseEnterPrevposeButton();
		}
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.name.Equals("NextPose"))
        {
            MouseEscapeNextposeButton();
        }
        else if (collision.name.Equals("ReStart"))
        {
            MouseEscapeRestartButton();
        }
        else if (collision.name.Equals("Start"))
        {
            MouseEscapeStartButton();
		} 
		else if (collision.name.Equals("PrevPose"))
		{
			MouseEscapePrevposeButton();
		}
    }

	public void MouseEscapePrevposeButton()
	{
		buttonEnterTime = 0.0f;
		ButtonSelector = 4;
	}

	public void MouseEnterPrevposeButton()
	{
		if (buttonEnterTime == 0.0f)
		{
			buttonEnterTime = Time.time;
			ButtonSelector = 0;
		}
	}

    public void MouseEscapeNextposeButton()
    {
        buttonEnterTime = 0.0f;
        ButtonSelector = 4;
    }

    public void MouseEnterNextposeButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 1;
        }
    }

    public void MouseEscapeRestartButton()
    {
        buttonEnterTime = 0.0f;
        ButtonSelector = 4;
    }

    public void MouseEnterRestartButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 2;
        }
    }

    public void MouseEscapeStartButton()
    {
        buttonEnterTime = 0.0f;
        ButtonSelector = 4;
    }

    public void MouseEnterStartButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 3;
        }
    }

    public void NextPose()
    {
		posDone.SetActive (false);
		TimerText.SetActive (false);	
		ButtonSelector = 1;
        poseNum++;
        if (poseNum > 3)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Pattern");
        }
        poseImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("Pose" + poseNum);
    }

	public void PrevPose()
	{
		posDone.SetActive (false);
		TimerText.SetActive (false);
		ButtonSelector = 0;
		poseNum--;
		if (poseNum <= 1)
			poseNum = 1;
		poseImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("Pose" + poseNum);
	}

    public void rePose()
    {
		
		ButtonSelector = 3;
        startPose();
    }

    public void startPose()
    {
		int CountfAu = 0;
		ButtonSelector = 2;
		startTrigger = true;
		posDone.SetActive (false);
		TimerText.SetActive (true);
        if (poseTime == 0.0f)
            poseTime = Time.time;
		float temp = 3.0f - (currentTime - poseTime);
		TimerText.GetComponent<Text>().text = "측정중입니다....::" + temp.ToString("F1");
        if (currentTime - poseTime > 1.0f && currentTime - poseTime < 3.0f)
        {
			if (KinectManager.AuToken) {
				if (poseNum == 1) {
					KinectGestures.AuXTrigger = true;
				}
				if (poseNum == 2) {
					KinectGestures.AuYTrigger = true;
				}
				if (poseNum == 3) {
					KinectGestures.AuZTrigger = true;
				}
			} else {
				if (poseNum == 1) {
					KinectGestures.initialXTrigger = true;	
					//TimerText.GetComponent<Text>().text = KinectManager.XMaximum + ":<X Maximum Value:::::X Minimum Value>:" + KinectManager.XMinimum;
				}
				if (poseNum == 2) {
					KinectGestures.initialYTrigger = true;
					//TimerText.GetComponent<Text>().text = KinectManager.YMaximum + ":<Y Maximum Value:::::Y Minimum Value>:" + KinectManager.YMinimum;
				}
				if (poseNum == 3) {
					KinectGestures.initialZTrigger = true;
					//TimerText.GetComponent<Text>().text = KinectManager.ZMaximum + ":<Z Maximum Value:::::Z Minimum Value>:" + KinectManager.ZMinimum;
				}
			}
        }
        else if (currentTime - poseTime > 3.0f)
		{
			if (KinectManager.AuToken && poseNum == 3) {
				if (System.Math.Abs (KinectManager.AuXMaximum - KinectManager.XMaximum) < 0.2) {
					Debug.Log (KinectManager.AuXMaximum + " : " + KinectManager.XMaximum + "        = " +System.Math.Abs (KinectManager.AuXMaximum - KinectManager.XMaximum));
					CountfAu++;
				}
				if (System.Math.Abs (KinectManager.AuXMinimum - KinectManager.XMinimum) < 0.2) {
					Debug.Log (KinectManager.AuXMinimum + " : " + KinectManager.XMinimum + "        = " +System.Math.Abs (KinectManager.AuXMinimum - KinectManager.XMinimum));
					CountfAu++;
				}
				if (System.Math.Abs (KinectManager.AuYMaximum - KinectManager.YMaximum) < 0.2) {
					Debug.Log (KinectManager.AuYMaximum + " : " + KinectManager.YMaximum + "        = " +System.Math.Abs (KinectManager.AuYMaximum - KinectManager.YMaximum));
					CountfAu++;
				}
				if (System.Math.Abs (KinectManager.AuYMinimum - KinectManager.YMinimum) < 0.2) {
					Debug.Log (KinectManager.AuYMinimum + " : " + KinectManager.YMinimum + "        = " +System.Math.Abs (KinectManager.AuYMinimum - KinectManager.YMinimum));
					CountfAu++;
				}
				if (System.Math.Abs (KinectManager.AuZMaximum - KinectManager.ZMaximum) < 0.2){
					Debug.Log (KinectManager.AuZMaximum + " : " + KinectManager.ZMaximum + "        = " +System.Math.Abs (KinectManager.AuZMaximum - KinectManager.ZMaximum));
					CountfAu++;
				}
				if (System.Math.Abs (KinectManager.AuZMinimum - KinectManager.ZMinimum) < 0.2) {
					Debug.Log (KinectManager.AuZMinimum + " : " + KinectManager.ZMinimum + "        = " +System.Math.Abs (KinectManager.AuZMinimum - KinectManager.ZMinimum));
					CountfAu++;
				}

				if (CountfAu > 4) {
					//Authorized user
					KinectManager.AuToken = false;
					UnityEngine.SceneManagement.SceneManager.LoadScene ("Main");

				} else {
					//Unauthorized! reset
					KinectGestures.ClearALL();
					Destroy (GameObject.Find("KinectManagerObject"));
					UnityEngine.SceneManagement.SceneManager.LoadScene ("Intro");
				}
			}


            startTrigger = false;
            poseTime = 0.0f;
			ButtonSelector = 4;
			posDone.SetActive (true);
        }
    }
}