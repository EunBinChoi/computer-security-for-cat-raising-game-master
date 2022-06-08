using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroCursor : MonoBehaviour
{
    GameObject infoPanel, Timer;
    float currentTime, buttonEnterTime;
	int ButtonSelector = 0;
	public static Vector3 rightWristVector;

    // Use this for initialization
    void Start()
    {
        infoPanel = GameObject.Find("infoPanel");
        infoPanel.SetActive(false);
        Timer = GameObject.Find("Timer");
        currentTime = buttonEnterTime = 0.0f;
    }

    // Update is called once per frame
    void Update()
	{
		KinectManager kinectManager = KinectManager.Instance;
		if ((!kinectManager || !kinectManager.IsInitialized () || !kinectManager.IsUserDetected ())) {
		} else {
			uint userId = kinectManager.GetPlayer1ID ();
			kinectManager.DetectGesture (userId, KinectGestures.Gestures.RaiseRightHand);
		}
        currentTime = Time.time;
        //3초 이상이면 전환, 버튼 1이면 Start고 버튼 2면 Exit
        if (buttonEnterTime != 0.0f)
        {
            Timer.GetComponent<Image>().fillAmount = (currentTime - buttonEnterTime) / 2.0f;

            if (currentTime - buttonEnterTime >= 2.0f)
            {
                if (ButtonSelector == 1)
                    SceneManager.LoadScene("Initialize");
                if (ButtonSelector == 2)
                    Application.Quit();
                else { }
            }
            else { }            
            //Debug.Log(Camera.main.ScreenToViewportPoint(transform.position).ToString("F4"));
        }
        else if (buttonEnterTime == 0.0f)
        {
            Timer.GetComponent<Image>().fillAmount = 0.0f;
		}

		SetMouseFunction ();
	}

	protected void SetMouseFunction(){
		Vector3 temp;
		rightWristVector = KinectGestures.rightWristVector;
		//Debug.Log (Camera.main.pixelWidth + ", " + Camera.main.pixelHeight);

		temp.x = Camera.main.pixelWidth/2 + rightWristVector.x*Camera.main.pixelWidth;
		temp.y = rightWristVector.y*Camera.main.pixelHeight - Camera.main.pixelHeight;
		temp.z = 0;

		transform.position = temp;
		//.transform.position.x = Camera.main.ScreenToViewportPoint(temp).x;
		//KinectCursur.transform.position.y = Camera.main.ScreenToViewportPoint(temp).y;
	}
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name.Equals("infoButton"))
        {
            MouseEnterinfoButton();
        }
        else if (collision.name.Equals("StartButton"))
        {
            MouseEnterStartButton();
        }
        else if (collision.name.Equals("ExitButton"))
        {
            MouseEnterExitButton();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.name.Equals("infoButton"))
        {
            MouseEscapeinfoButton();
        }
        else if (collision.name.Equals("StartButton"))
        {
            MouseEscapeStartButton();
        }
        else if (collision.name.Equals("ExitButton"))
        {
            MouseEscapeExitButton();
        }
    }

    // Button 보면 인스펙터에 Event Trigger라고 하는 부분에서 오버레이 처리함.

    public void MouseEscapeStartButton()
    {
        buttonEnterTime = 0.0f;

    }

    public void MouseEnterStartButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 1;
        }
    }

    public void MouseEscapeExitButton()
    {
        buttonEnterTime = 0.0f;

    }

    public void MouseEnterExitButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 2;
        }
    }

	void Awake(){
		KinectManagementScript.patternNumber = 0;
	}


    // 충돌감지랑 비슷함. 마우스 Enter면 켜지고 Escape면 안 켜짐

    public void MouseEscapeinfoButton()
    {
        infoPanel.SetActive(false);
    }

    public void MouseEnterinfoButton()
    {
        infoPanel.SetActive(true);
    }
}