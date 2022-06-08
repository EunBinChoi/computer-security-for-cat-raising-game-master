using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum CatState { sleep, idle, wash, walk, run, play, pole }

public class Cat : MonoBehaviour
{
    public Animator anim;
	GameObject Laserpoint;
    float FriendlyTimer, CurrentTime, AnimationTime, UserInputTime, PlayTime, ResponseTime;
    public NavMeshAgent agent;
    public GameObject emo, food;
    public bool jump;
    private OffMeshLinkData offMeshLinkData;
    Vector3 jumpStartPos = Vector3.zero;
    float jumpSpeed = 2.0f;
    float jumpHeight = -0.8f;
    float jumpDistance = 0.0f;
    float jumpTotalTime = 0.0f;
    float jumpDeltaTime = 0.0f;
    private int select_behavior = 0;
    private bool onTriggerYarn = false, onTriggerScratter = false;
    GameObject YarnBall, Scratter;
    AnimatorStateInfo info;
    AnimatorTransitionInfo info2;
    bool isOffMeshLinkComplete = false;
    Vector3 distanceVector;

    public void set_select_behavior(int a)
    {
        select_behavior = a;
    }

	void Awake(){
		Laserpoint = GameObject.Find ("HitPoint");
	}

    // Use this for initialization
    void Start()
    {
        anim = GetComponent<Animator>();
        YarnBall = GameObject.FindGameObjectWithTag("YarnBall");
        Scratter = GameObject.Find("cu_cat2_pole_mesh");
        FriendlyTimer = CurrentTime = AnimationTime = UserInputTime = PlayTime = ResponseTime = 0.0f;
        FriendlyTimer = Time.time;
        UserInputTime = 0.0f;
        agent = GetComponent<NavMeshAgent>();
        set_select_behavior(Random.Range(0, 7));
    }
    // Update is called once per frame
    void Update()
    {
        CurrentTime = Time.time;

        info = anim.GetCurrentAnimatorStateInfo(0);   //현재 애니메이션 상태
        info2 = anim.GetAnimatorTransitionInfo(0);   //현재 트랜지션 상태
                                                     //anim.runtimeAnimatorController = Resources.Load("") as RuntimeAnimatorController; //애니메이터 변경

        //점프
        if (agent.isOnOffMeshLink)
        {
            int i = 0;
            GameObject[] OffMeshLinkSofa = GameObject.FindGameObjectsWithTag("Sofa");
            GameObject[] OffMeshLinkPlane = GameObject.FindGameObjectsWithTag("Plane");

            if (!jump)
            {
                offMeshLinkData = agent.currentOffMeshLinkData;
                jump = true;

                jumpStartPos = transform.position;

                Vector3 dirToJump = offMeshLinkData.endPos - jumpStartPos;

                jumpDistance = dirToJump.magnitude;

                jumpTotalTime = jumpDistance / jumpSpeed;

                jumpDeltaTime = 0.0f;

                dirToJump.y = 0.0f;
                dirToJump *= 1.0f / jumpDistance;

                agent.isStopped = true;

                for (i = 0; i < OffMeshLinkSofa.Length; i++)
                {
                    if (Vector3.Distance(offMeshLinkData.startPos, OffMeshLinkSofa[i].transform.position) <= 0.1f)
                    {
                        if(!info.IsName("A_jump_start"))
                            anim.SetTrigger("Jump");
                        anim.SetBool("JumpDown", true);
                        anim.SetBool("JumpUp", false);
                        break;
                    }
                }
                for (i = 0; i < OffMeshLinkPlane.Length; i++)
                {
                    if (Vector3.Distance(offMeshLinkData.startPos, OffMeshLinkPlane[i].transform.position) <= 0.1f)
                    {
                        if (!info.IsName("A_jump_start"))
                            anim.SetTrigger("Jump");
                        anim.SetBool("JumpUp", true);
                        anim.SetBool("JumpDown", false);
                        break;
                    }
                }
            }
            else
            {
                jumpDeltaTime += Time.deltaTime;
                float factor = jumpDeltaTime / jumpTotalTime;

                if (anim.GetBool("JumpUp"))
                {
                    if (factor >= 1.0f)
                    {                        
                        isOffMeshLinkComplete = true;
						anim.SetBool("JumpUp", false);
                    }     
                    else if(factor >= 0.8f)
                    {
                        anim.SetTrigger("JumpEnd");
                    }
                }
                else if (anim.GetBool("JumpDown"))
                {
                    if (factor >= 1.0f)
                    {
                        isOffMeshLinkComplete = true;
						anim.SetBool("JumpDown", false);
                    }
                    else if(factor >= 0.8f)
                    {
                        anim.SetTrigger("JumpEnd");
                    }
                }
                if (isOffMeshLinkComplete)
                {
					
                    transform.position = offMeshLinkData.endPos;

                    agent.CompleteOffMeshLink();
                    agent.isStopped = false;                  
                    jump = false;
                    isOffMeshLinkComplete = false;
				}
                else
                {
                    if (factor >= 1.0f)
                    {
                        isOffMeshLinkComplete = true;
                    }
                    Vector3 pos = Vector3.Lerp(jumpStartPos, offMeshLinkData.endPos, factor);
                    GameObject target = GameObject.Find("Target");
                    target.transform.position = offMeshLinkData.endPos;
                    Vector3 vec = Vector3.zero;
                    if (anim.GetBool("JumpDown"))
                    {
                        jumpHeight = -0.8f;
                        pos.y -= Mathf.Sin(Mathf.PI * factor) * jumpHeight;
                    }
                    else if (anim.GetBool("JumpUp"))
                    {
                        jumpHeight = 0.8f;
                        pos.y += Mathf.Sin(Mathf.PI * factor) * jumpHeight;
                    }
                    transform.position = pos;
                    vec = (target.transform.position - transform.position).normalized;
                    Quaternion toQuaternion = Quaternion.LookRotation(vec);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toQuaternion, 3.0f * Time.deltaTime);
                }
            }
        }

        KinectManager kinectManager = KinectManager.Instance;
        bool ready = Server.readyToUser;
        //유저입력대기
        if (anim.GetBool("UserInput"))
        {
            anim.SetBool("Run", false);
            anim.SetBool("Walk", false);
            anim.SetBool("Sleep", false);
            anim.SetBool("Play", false);
            anim.SetBool("Wash", false);
			/*if (!info.IsName("B_idle"))
            {
                anim.SetTrigger("B_idle");
			}*/
			if (!info.IsName("B_idle"))
			{
				if (info.normalizedTime >= 0.8f)
					anim.SetTrigger("B_idle");
			}

            if ((CurrentTime - UserInputTime >= 60.0f && UserInputTime != 0.0f) || !kinectManager.IsUserDetected())
            {
                anim.SetBool("UserInput", false);
                anim.SetBool("B_idle", false);
                anim.SetBool("Hungry", false);

                set_select_behavior(Random.Range(0, 7));

                UserInputTime = 0.0f;
            }
        }
        else
        {

            if (select_behavior == (int)CatState.sleep)
            {
                if (!agent.pathPending)
                {
                    anim.SetBool("Sleep", true);
                    anim.SetBool("Run", false);
                    anim.SetBool("Walk", false);
                    anim.SetBool("Play", false);
                }
                if (CurrentTime - AnimationTime > 10.0f)
                {
                    anim.SetBool("Sleep", false);
                    set_select_behavior(Random.Range(0, 7));

                }
            }
            else if (select_behavior == (int)CatState.idle)
            {
                if (!agent.pathPending)
                {
                    anim.SetBool("Run", false);
                    anim.SetBool("Walk", false);
                    anim.SetBool("Play", false);
                    anim.SetBool("Sleep", false);
                }
                if (CurrentTime - AnimationTime > 5.0f)
                {
                    set_select_behavior(Random.Range(0, 7));

                    agent.SetDestination(new Vector3(Random.Range(-600, 700) * 0.01f, 0, Random.Range(-200, 500) * 0.01f));

                    anim.SetBool("Run", false);
                    anim.SetBool("Walk", false);
                    anim.SetBool("Play", false);
                    anim.SetBool("Sleep", false);
                }
            }
            else if (select_behavior == (int)CatState.wash)
            {
                agent.velocity = new Vector3(0, 0, 0);
                agent.speed = 0;
                agent.isStopped = true;
                anim.SetBool("Run", false);
                anim.SetBool("Walk", false);
                anim.SetBool("Play", false);
                anim.SetBool("Sleep", false);
                anim.SetBool("Wash", true);
            }
            else if (select_behavior == (int)CatState.walk)
            {
                if (!agent.pathPending)
                {
                    if (agent.remainingDistance <= 0.1f)
                    {
                        set_select_behavior(Random.Range(0, 7));
						if (!Laserpoint.GetComponent<LineRenderer> ().enabled) {
							agent.SetDestination (new Vector3 (Random.Range (-600, 700) * 0.01f, 0, Random.Range (-200, 500) * 0.01f));
						}
                    }
                    agent.speed = 1;
                    anim.SetBool("Run", false);
                    anim.SetBool("Walk", true);
                    anim.SetBool("Play", false);
                    anim.SetBool("Sleep", false);
                }
            }
            else if (select_behavior == (int)CatState.run)
            {
                if (!agent.pathPending)
                {
                    if (agent.remainingDistance <= 0.1f)
                    {
                        set_select_behavior(Random.Range(0, 7));
						if (!Laserpoint.GetComponent<LineRenderer> ().enabled) {
							agent.SetDestination (new Vector3 (Random.Range (-600, 700) * 0.01f, 0, Random.Range (-200, 500) * 0.01f));
						}
					}
                    agent.speed = 2;
                    anim.SetBool("Run", true);
                    anim.SetBool("Walk", false);
                    anim.SetBool("Play", false);
                    anim.SetBool("Sleep", false);
                }
            }

            else if (select_behavior == (int)CatState.play)
            {
                agent.SetDestination(YarnBall.transform.position);
                if (onTriggerYarn)
                {
                    anim.SetBool("Run", false);
                    agent.velocity = new Vector3(0, 0, 0);
                    agent.speed = 0;
                    agent.isStopped = true;
                    anim.SetBool("Play", true);

                }
                else
                {
                    if (!agent.pathPending)
                    {
                        agent.speed = 2;
                        anim.SetBool("Run", true);
                        anim.SetBool("Walk", false);
                        anim.SetBool("Play", false);
                        anim.SetBool("Sleep", false);
                    }						
                }               
            }
            else //if(select_behavior == (int)CatState.pole)
            {
                agent.SetDestination(Scratter.transform.position);

                if (onTriggerScratter)
                {
                    anim.SetBool("Run", false);
                    agent.velocity = new Vector3(0, 0, 0);
                    agent.speed = 0;
                    agent.isStopped = true;
                    anim.SetBool("Polling", true);
                    Vector3 vec = (Scratter.transform.position - transform.position).normalized;
                    Quaternion toQuaternion = Quaternion.LookRotation(vec);
                    transform.rotation = Quaternion.Slerp(transform.rotation, toQuaternion, 3.0f * Time.deltaTime);
                }
                else
                {
                    if (!agent.pathPending)
                    {
                        agent.speed = 2;
                        anim.SetBool("Run", true);
                        anim.SetBool("Walk", false);
                        anim.SetBool("Play", false);
                        anim.SetBool("Sleep", false);
                    }
                }
                
            }

            if ((info.IsName("A_walk") || info.IsName("A_run")) && agent.remainingDistance <= 0.1f)
            {
                if (!agent.pathPending)
                {
                    set_select_behavior(Random.Range(0, 7));

					if (!Laserpoint.GetComponent<LineRenderer> ().enabled) {
						agent.SetDestination (new Vector3 (Random.Range (-600, 700) * 0.01f, 0, Random.Range (-200, 500) * 0.01f));
					}
				}
            }
        }
		Debug.Log (onTriggerYarn);

        if (info.IsName("B_idle") || info.IsName("B_wash") || info.IsName("D_chodai") || info.IsName("B_cry") || info.IsName("C_wiggle"))
        {
            agent.isStopped = true;
            agent.SetDestination(transform.position);
        }

        if (info2.IsName("A_pole_loop -> A_pole_end"))
        {
            agent.SetDestination(new Vector3(Random.Range(-600, 700) * 0.01f, Random.Range(0, 200) * 0.01f, Random.Range(-200, 500) * 0.01f));
        }

        if (info2.IsName("B_idle -> BtoA") || info2.IsName("B_picks -> B_idle 0") || info2.IsName("A_eat -> A_idle") || info2.IsName("parts_ear_stand -> A_idle") || info2.IsName("CtoA -> A_idle"))
        {
            set_select_behavior(Random.Range(0, 7));

        }

        if (info2.IsName("AnyState -> B_idle"))
        {
            if (UserInputTime == 0.0f)
            {
                UserInputTime = Time.time;
            }
        }

        if (info.IsName("A_idle") || info.IsName("C_sleep") || info.IsName("B_idle"))
        {
            agent.speed = 0;
            agent.velocity = new Vector3(0, 0, 0);
            agent.isStopped = true;
        }

        if (info.IsName("A_walk") || info.IsName("A_run"))
        {
            emo.SetActive(false);
            food.SetActive(false);
            agent.speed = 1;
            agent.isStopped = false;
            if (jump) jump = false;
			onTriggerYarn = false;
			onTriggerScratter = false;
        }

        if (info.IsName("A_pole_loop"))
        {
            transform.LookAt(new Vector3(Scratter.transform.position.x, transform.position.y, Scratter.transform.position.z));

			agent.speed = 0;
			agent.isStopped = true;

            if (CurrentTime - PlayTime >= 5.0f && PlayTime != 0.0f)
            {
                anim.SetBool("Polling", false);
                set_select_behavior(Random.Range(0, 7));
                Debug.Log(PlayTime);
                agent.SetDestination(new Vector3(Random.Range(-600, 700) * 0.01f, 0, Random.Range(-200, 500) * 0.01f));
                PlayTime = 0.0f;
                onTriggerScratter = false;
            }
        }

        if (info.IsName("A_punch_R"))
        {
            if (info.normalizedTime >= 0.4f && Vector3.Distance(transform.position, YarnBall.transform.position) <= 1.5f)
            {
                YarnBallScript yarnballscript = YarnBall.GetComponent<YarnBallScript>();

                if (!yarnballscript.collision)
                {
                    YarnBall.transform.Translate(distanceVector.normalized * 0.5f * Time.deltaTime);
                    YarnBall.transform.Rotate(distanceVector.normalized * 200.0f * Time.deltaTime);
                    YarnBall.transform.position = new Vector3(YarnBall.transform.position.x, 0.3f, YarnBall.transform.position.z);
                }
            }
			if (info.normalizedTime >= 1.0f) {
				anim.SetBool ("Play", false);
				set_select_behavior (Random.Range (0, 7));
				agent.SetDestination (new Vector3 (Random.Range (-600, 700) * 0.01f, 0, Random.Range (-200, 500) * 0.01f));
				PlayTime = 0.0f;
				onTriggerYarn = false;
			}
        }

        if (info.IsName("A_jump_down") && transform.position.y <= 0.05f)
        {
            anim.SetBool("JumpDown", false);
        }

        if (info2.IsName("C_idle -> C_sleep") || info2.IsName("A_walk -> A_idle") || info2.IsName("A_run -> A_idle") || info2.IsName("C_idle -> A_idle"))
        {
            AnimationTime = Time.time;
        }

        if (info2.IsName("A_idle -> A_punch_R") || info2.IsName("AnyState -> A_pole_start"))
        {
            PlayTime = Time.time;
        }
		Debug.Log (PlayTime);

        if (info2.IsName("B_picks -> B_idle 0"))
        {
            anim.SetBool("Wash", false);
            set_select_behavior(Random.Range(0, 7));
        }

        if (info2.IsName("AnyState -> parts_ear_down") || info2.IsName("D_idle -> D_chodai"))
        {
            ResponseTime = Time.time;
        }

        if (info.IsName("parts_ear_stand") || info2.IsName("D_chodai"))
        {
            if (CurrentTime - ResponseTime >= 5.0f)
            {
                anim.SetBool("EarDown", false);
                anim.SetBool("EarUp", false);
                anim.SetBool("Hungry", false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("YarnBall") && select_behavior == (int)CatState.play)
        {
            onTriggerYarn = true;
            distanceVector = transform.position - YarnBall.transform.position;
            distanceVector.Normalize();
            Debug.Log(distanceVector);
            distanceVector = new Vector3(distanceVector.z, distanceVector.y, -distanceVector.x);
            Debug.Log(distanceVector);
			anim.SetBool ("Run", false);
			anim.SetBool ("Walk", false);
        }
        if (other.tag.Equals("Scratter") && select_behavior == (int)CatState.pole)
        {
            onTriggerScratter = true;
            anim.SetTrigger("PoleStart");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("YarnBall") && select_behavior == (int)CatState.play)
        {
            if (info.IsName("A_idle"))
            {
                transform.LookAt(YarnBall.transform.position);
            }
            if (!onTriggerYarn)
                onTriggerYarn = true;
        }
        if (other.tag.Equals("Scratter") && select_behavior == (int)CatState.pole)
        {
            if (info.IsName("A_pole_loop"))
            {
                if (CurrentTime - PlayTime >= 10.0f && PlayTime != 0.0f)
                {
                    anim.SetBool("Polling", false);
                    set_select_behavior(Random.Range(0, 7));
                    Debug.Log(PlayTime);
                    agent.SetDestination(new Vector3(Random.Range(-600, 700) * 0.01f, 0, Random.Range(-200, 500) * 0.01f));
                    PlayTime = 0.0f;
                    onTriggerScratter = false;
                }
            }
        }
    }

	private void OnTriggerExit(Collider other)
	{
		if (other.tag.Equals("YarnBall") && select_behavior == (int)CatState.play)
		{
			onTriggerYarn = false;
			anim.SetBool ("Play", false);
		}
		if (other.tag.Equals("Scratter") && select_behavior == (int)CatState.pole)
		{
			onTriggerScratter = false;
			anim.SetBool ("Polling", false);
		}
	}
}
