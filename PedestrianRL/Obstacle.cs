using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float dangerLevel = 0f;
    public float size = 1f;

    public bool isActive = true;

    public void Clone(GameObject obstacle)
    {

        this.dangerLevel = obstacle.GetComponent<Obstacle>().dangerLevel;
        this.size = obstacle.GetComponent<Obstacle>().size;
        this.isActive = obstacle.GetComponent<Obstacle>().isActive;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
