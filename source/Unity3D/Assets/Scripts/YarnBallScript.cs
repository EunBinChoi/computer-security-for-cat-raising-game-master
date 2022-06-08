using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YarnBallScript : MonoBehaviour
{
    public GameObject Cat;
    public bool collision = false;
    public Vector3 distanceVector;
    float CurrentTime, CollisionTime;

    private void Awake()
    {
        Cat = GameObject.Find("cu_cat2_model");
        CurrentTime = CollisionTime = 0.0f;
    }

    private void Update()
    {
        CurrentTime = Time.time;
        if (collision)
        {
            transform.Translate(-distanceVector * 0.5f * Time.deltaTime);
            transform.Rotate(-distanceVector * 300.0f * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
            if (CurrentTime - CollisionTime >= 3.0f && CollisionTime != 0.0f)
            {
                CollisionTime = 0.0f;
                collision = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Wall") || other.tag.Equals("Scratter"))
        {
            Debug.Log("TriggerEnter");
            collision = true;
            CollisionTime = Time.time;
            distanceVector = -(transform.position - other.transform.position);
            distanceVector.Normalize();
        }
    }
}
