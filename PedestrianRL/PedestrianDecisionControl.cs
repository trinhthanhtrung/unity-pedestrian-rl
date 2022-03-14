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
public class PedestrianDecisionControl : MonoBehaviour
{
    public AgentRoute agentRoute;
    public Transform direction; 
    public float desiredSpeed = 0.4f;
    public bool showCurrentTarget = false;
    private GameObject currentRouteDest;
    private int currentRouteIndex;

    public GameObject obstacle;

    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<PedestrianRoutePlanRL>().enabled = true;
        this.GetComponent<PedestrianInteractingRL>().enabled = false;
        this.GetComponent<MovementPreditor>().enabled = false;
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (!obstacle)
            obstacle = GameObject.FindGameObjectWithTag("Obstacle");
        
        this.GetComponent<NavMeshAgent>().speed = desiredSpeed;

        if (obstacle && IsCloseTo(this.transform, obstacle.transform, 16f))
        {
            // Change to Interacting task
            this.GetComponent<PedestrianInteractingRL>().enabled = true;
            if (this.GetComponent<PedestrianInteractingRL>().obstaclePredictionMode == ObstaclePredictionMode.MovementPrediction)
                this.GetComponent<MovementPreditor>().enabled = true;
            // Assign current direction 
            if (currentRouteIndex > 9)
                this.GetComponent<PedestrianInteractingRL>().currentDestination = 
                    this.agentRoute.activeRoute[11].transform;
            else
                this.GetComponent<PedestrianInteractingRL>().currentDestination = 
                    this.agentRoute.activeRoute[currentRouteIndex + 2].transform;
        }
        else
        {
            // Disable Interacting task
            this.GetComponent<PedestrianInteractingRL>().enabled = false;
            if (this.GetComponent<PedestrianInteractingRL>().obstaclePredictionMode == ObstaclePredictionMode.MovementPrediction)
                this.GetComponent<MovementPreditor>().enabled = false;
        }

        if (this.GetComponent<PedestrianRoutePlanRL>().IsDonePlanning())
        {
            this.GetComponent<PedestrianRoutePlanRL>().enabled = false;
            FollowPath();
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
    }

    private void FollowPath()
    {
        if (IsCloseTo(this.transform, currentRouteDest.transform, 0.4f)) // Reach next target
        {
            // Hide target if needed
            currentRouteDest.GetComponent<Renderer>().enabled = false;

            if (currentRouteIndex == agentRoute.activeRoute.Length - 1) // Reach final target
            {
                desiredSpeed = 0;
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

    //TODO May be to create a utility class with all the static functions?
    private bool IsCloseTo(Transform object1, Transform object2, float sqrDist)
    {
        Vector2 obj1Pos = new Vector2(object1.position.x, object1.position.z);
        Vector2 obj2Pos = new Vector2(object2.position.x, object2.position.z);
        if (Vector2.SqrMagnitude(obj1Pos - obj2Pos) > sqrDist)
            return false;
        else
            return true;
    }

}
