using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeRewind : MonoBehaviour
{
    public float recordTime = 5f;
    private bool isRewinding = false;

    private List<PointInTime> pointsInTime;
    private Rigidbody2D rb;

    void Start()
    {
        pointsInTime = new List<PointInTime>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            StartRewind();

        if (Input.GetKeyUp(KeyCode.Return))
            StopRewind();
    }

    void FixedUpdate()
    {
        if (isRewinding)
            Rewind();
        else
            Record();
    }

    void Rewind()
    {
        if (pointsInTime.Count > 0)
        {
            PointInTime pointInTime = pointsInTime[0];

            transform.position = pointInTime.position;
            transform.rotation = pointInTime.rotation;

            PlayerInteract.Instance.SetTime(pointInTime.gameTime);

            pointsInTime.RemoveAt(0);
        }
        else
        {
            StopRewind();
        }
    }

    void Record()
    {
        pointsInTime.Insert(0, new PointInTime(transform.position, transform.rotation, PlayerInteract.Instance.GetExactTime()));

        if (pointsInTime.Count > Mathf.Round(recordTime / Time.fixedDeltaTime))
        {
            pointsInTime.RemoveAt(pointsInTime.Count - 1);
        }
    }

    public void StartRewind()
    {
        isRewinding = true;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        PlayerInteract.Instance.IsRewinding = true;
    }
    
    public void StopRewind()
    {
        isRewinding = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        PlayerInteract.Instance.IsRewinding = false;
    }
}
