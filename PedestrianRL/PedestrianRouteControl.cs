using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

/// <summary>
/// This class is to control the character to move along the predefined route with
/// the speed specified. 
/// An AIThirdPersonCharacter placed on a NavMesh is required.
/// </summary>
public class PedestrianRouteControl : MonoBehaviour
{
    public AgentRoute agentRoute;
    public float currentSpeed = 0.4f;
    public bool showCurrentTarget = false;
    private GameObject currentRouteDest;
    private int currentRouteIndex;

    // Start is called before the first frame update
    void Start()
    {
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        this.GetComponent<NavMeshAgent>().speed = currentSpeed;

        if (Vector3.SqrMagnitude(this.transform.position - currentRouteDest.transform.position) < 2.25f) // Reach next target
        {
            // Hide target
            currentRouteDest.GetComponent<Renderer>().enabled = false;

            if (currentRouteIndex == agentRoute.activeRoute.Length - 1) // Reach final target
            {
                currentSpeed = 0;
                //currentRouteDest = agentRoute.activeRoute[0].gameObject;
                //this.GetComponent<AICharacterControl>().target = currentRouteDest.transform;
            }
            else
            {
                currentRouteIndex++;
                currentRouteDest = agentRoute.activeRoute[currentRouteIndex].gameObject;
                this.GetComponent<AICharacterControl>().target = currentRouteDest.transform;

                // Show current target
                currentRouteDest.GetComponent<Renderer>().enabled = showCurrentTarget;
            }
        }
    }

    public bool DestinationReached()
    {
        return (currentRouteIndex == agentRoute.activeRoute.Length - 1);
    }

    public void Reset()
    {
        currentRouteIndex = 0;
        currentRouteDest = agentRoute.activeRoute[0].gameObject;
        this.GetComponent<AICharacterControl>().target = currentRouteDest.transform;

        // Show current target
        agentRoute.activeRoute[0].gameObject.GetComponent<Renderer>().enabled = showCurrentTarget;

        currentSpeed = 0.4f;
    }
}
