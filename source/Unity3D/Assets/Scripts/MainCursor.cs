using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainCursor : MonoBehaviour {
    GameObject MenuPanel, Timer;
    float currentTime, buttonEnterTime;
    int ButtonSelector = 0;
    public bool MenuActivate = false;
	public static Vector3 rightWristVector;
	public GameObject Laserpoint;
    // Use this for initialization
    void Start () {
        MenuPanel = GameObject.Find("MenuPanel");
        Timer = GameObject.Find("Timer");
        
        currentTime = buttonEnterTime = 0.0f;
        MenuActivate = MenuPanel.activeSelf;
        MenuPanel.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () 
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
				if (ButtonSelector == 1) {
					KinectGestures.ClearALL();

					Destroy (GameObject.Find("KinectManagerObject"));
					SceneManager.LoadScene ("Intro");
				}
                if (ButtonSelector == 2)
                    Application.Quit();
                if (ButtonSelector == 3)
                    ResumeGame();
				if (ButtonSelector == 4) {
					//GameObject.Find ("EventSystem").GetComponent<Event> ().LaserToggle ();
					if(Laserpoint.GetComponent<LineRenderer>().enabled)
					{
						Laserpoint.GetComponent<LineRenderer>().enabled = false;
					}
					else if (!Laserpoint.GetComponent<LineRenderer>().enabled)
					{
						Laserpoint.GetComponent<LineRenderer>().enabled = true;
						MenuPanel.SetActive (false);
					}
					buttonEnterTime = 0.0f;
				}
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
		temp.y = rightWristVector.y * Camera.main.pixelHeight/3;
		temp.z = 0;

		transform.position = temp;
		//.transform.position.x = Camera.main.ScreenToViewportPoint(temp).x;
		//KinectCursur.transform.position.y = Camera.main.ScreenToViewportPoint(temp).y;
	}

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name.Equals("ToIntro"))
        {
            MouseEnterToIntroButton();
        }
        else if (collision.name.Equals("Quit"))
        {
            MouseEnterQuitButton();
        }
        else if (collision.name.Equals("ExitImage"))
        {
            MouseEnterExitButton();
            Debug.Log("ExitTrigger");
        }
		else if (collision.name.Equals("LaserImage"))
		{
			MouseEnterLaserButton();
		}
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.name.Equals("ToIntro"))
        {
            MouseEscapeToIntroButton();
        }
        else if (collision.name.Equals("Quit"))
        {
            MouseEscapeQuitButton();
        }
        else if (collision.name.Equals("ExitImage"))
        {
            MouseEscapeExitButton();
            
        }
		else if (collision.name.Equals("LaserImage"))
		{
			MouseEscapeLaserButton();
		}
    }

	public void MouseEscapeLaserButton()
	{
		buttonEnterTime = 0.0f;

	}

	public void MouseEnterLaserButton()
	{
		if (buttonEnterTime == 0.0f)
		{
			buttonEnterTime = Time.time;
			ButtonSelector = 4;
		}
	}

    public void MouseEscapeToIntroButton()
    {
        buttonEnterTime = 0.0f;

    }

    public void MouseEnterToIntroButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 1;
        }
    }

    public void MouseEscapeQuitButton()
    {
        buttonEnterTime = 0.0f;

    }

    public void MouseEnterQuitButton()
    {
        if (buttonEnterTime == 0.0f)
        {
            buttonEnterTime = Time.time;
            ButtonSelector = 2;
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
            ButtonSelector = 3;
        }
    }

    // 게임 재개
    void ResumeGame()
    {
        MenuPanel.SetActive(false);
    }
}
