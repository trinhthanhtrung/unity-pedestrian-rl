using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMovingObstacle : Obstacle
{
    public Direction obstacleDirection = Direction.DOWN;
    public Transform directionObject;
    public float obstacleSpeed = 0.4f;

    public bool randomDirectionChange = true;
    [Range(0f, 10f)]
    public float minChangeTime = 3f;
    [Range(1f, 30f)]
    public float maxChangeTime = 10f;
    [Range(0f, 180f)]
    public float maxAngle = 30f;

    private bool directionChangeWait = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (randomDirectionChange)
        {
            if (!directionObject)
                Debug.LogError("A direction object is required for random direction change.");


            if (!directionChangeWait)
            {
                // Set new position for direction object so the angle is rotated by randomAngle
                float randomAngle = Random.Range(-maxAngle, maxAngle);
                Vector3 origAngleVector = directionObject.transform.position - this.transform.position;
                Vector3 newAngleVector = Quaternion.AngleAxis(randomAngle, Vector3.up) * origAngleVector;
                directionObject.transform.position = this.transform.position + newAngleVector;

                StartCoroutine(DirectionChangeWaiting(Random.Range(minChangeTime, maxChangeTime)));
            }

        }
    }

    IEnumerator DirectionChangeWaiting(float durationInSecond)
    {
        directionChangeWait = true;
        yield return new WaitForSeconds(durationInSecond);
        directionChangeWait = false;
    }

}
