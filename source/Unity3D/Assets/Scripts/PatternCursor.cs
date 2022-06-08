using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PatternCursor : MonoBehaviour
{
    float currentTime, buttonEnterTime;
    int ButtonSelector = 0;
    GameObject Cat, Timer;
	public static int patternNum = 1;
	public static Vector3 rightWristVector;

    // Use this for initialization
    void Start()
    {
        currentTime = buttonEnterTime = 0.0f;
        Cat = GameObject.Find("cu_cat2_mesh");
        Timer = GameObject.Find("Timer");
        Cat.GetComponent<Renderer>().material.mainTexture = Resources.Load("cu_cat2_" + patternNum) as Texture2D;
    }

    // Update is called once per frame
    void Update()
	{
		KinectManagementScript.patternNumber = patternNum;
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
            Timer.GetComponent<Image>().fillAmount = (currentTime - buttonEnterTime)/2.0f;

            if (currentTime - buttonEnterTime >= 2.0f)
            {               
				if (ButtonSelector == 1)
					PrevPattern ();
				else if (ButtonSelector == 2)
					NextPattern ();
				else if (ButtonSelector == 3) {
					KinectGestures.readyTrigger = true;
					SceneManager.LoadScene ("Main");
				}
                else { }
            }
            else { } 
        }
        else if (buttonEnterTime == 0.0f)
        {
            Timer.GetComponent<Image>().fillAmount = 0.0f;
        }
		//Debug.Log(Camera.main.ScreenToViewportPoint(transform.position).ToString("F4"));SetMouseFunction ();
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
        if (collision.name.Equals("PrevButton"))
        {
            MouseEnterPrevButton();
        }
        else if (collision.name.Equals("NextButton"))
        {
            MouseEnterNextButton();
        }
        else if (collision.name.Equals("StartButton"))
        {
            MouseEnterStartButton();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.name.Equals("PrevButton"))
        {
            MouseEscapePrevButton();
        }
        else if (collision.name.Equals("NextButton"))
        {
            MouseEscapeNextButton();
        }
        else if (collision.name.Equals("StartButton"))
        {
            MouseEscapeStartButton();
        }
    }

    // Button 보면 인스펙터에 Event Trigger라고 하는 부분에서 오버레이 처리함.

    public void MouseEscapePrevButton()
    {
        buttonEnterTime = 0.0f;
        ButtonSelector = 0;
    }

    public void MouseEnterPrevButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 1;
        }
    }

    public void MouseEscapeNextButton()
    {
        buttonEnterTime = 0.0f;
        ButtonSelector = 0;

    }

    public void MouseEnterNextButton()
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
        ButtonSelector = 0;

    }

    public void MouseEnterStartButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 3;
        }
    }
    //털 패턴
    public void NextPattern()
    {
        patternNum++;
        if (patternNum >= 16) patternNum = 1;
        Cat.GetComponent<Renderer>().material.mainTexture = Resources.Load("cu_cat2_" + patternNum) as Texture2D;
        buttonEnterTime = Time.time;

    }

    public void PrevPattern()
    {
        patternNum--;
        if (patternNum <= 0) patternNum = 15;
        Cat.GetComponent<Renderer>().material.mainTexture = Resources.Load("cu_cat2_" + patternNum) as Texture2D;
        buttonEnterTime = Time.time;

    }
}