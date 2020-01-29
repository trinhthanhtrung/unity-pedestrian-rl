using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianObstacle : Obstacle
{
    public GameObject pedestrianObstacleAgent;

    const float ENV_LENGTH = 22f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        PedestrianDest movingObsEndNode = pedestrianObstacleAgent.GetComponent<PedestrianRouteControl>().agentRoute.activeRoute[11];
        movingObsEndNode.transform.localPosition = new Vector3(GetPredictedDestinationX(), 0.5f, -12f);
    }

    public AgentRoute GetPredictedRoute()
    {
        return pedestrianObstacleAgent.GetComponent<PedestrianRouteControl>().agentRoute;
    }

    public float GetPredictedDestinationX()
    {
        // Formula
        // ||        o    ||       * - velocity (x_v,z_v) 
        // ||       *     ||        local (1,4)                  L * x_v
        // ||             ||        world (-1,-4)       x = x0 - _______     
        // ||    .        ||                                       z_v

        Vector3 movingObsPos = pedestrianObstacleAgent.transform.localPosition;
        Vector3 velocityVector;
        if (pedestrianObstacleAgent.GetComponent<PedestrianRouteControl>())
            velocityVector = pedestrianObstacleAgent.GetComponent<PedestrianRouteControl>().direction.localPosition;
        else
        {
            Debug.Log("WARNING: Obstacle's velocity vector has NOT been set. Default to (0,1).");
            velocityVector = new Vector3(0, 0, 1);
        }
        // Check if moving obs is moving away from agent 
        // Assuming obs is heading toward agent. Otherwise needs to flip the environment
        if (velocityVector.z <= 0)
        {
            Debug.LogError("Invalid moving obstacle velocity vector.");
        }
        else
        {
            float destX = movingObsPos.x - ENV_LENGTH * velocityVector.x / velocityVector.z;
            // Check if the destination is out of bounds
            if (destX < -5) destX = -5;
            else if (destX > 5) destX = 5;
            return destX;
        }

        return -5;
    }

}
