using MLAgents;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is for reinforcement learning on a moving obstacle.
/// This is a simplified versionn of PedestrianRoutePlanRL class,
/// however most of the operation is flipped because of the direction
/// of the moving obstacle.
/// NOTE: This is for demonstrating ONLY. NOT suitable for training.
/// </summary>
public class PedestrianObsRuoteRL : Agent
{
    public PedestrianEnd endNode;

    private AgentRoute agentRoute;
    private int noOfRouteNodes;

    private Vector3 startingPosition;

    private Direction pedestrianObstacleDirection = Direction.DOWN;

    void OnDrawGizmosSelected()
    {
        // Draw a the route path when selected in Unity
        agentRoute = this.GetComponent<PedestrianDecisionControl>().agentRoute;
        // Draw the line from agent to direction vector object if exists
        if (this.GetComponent<PedestrianDecisionControl>().direction != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.transform.position, this.GetComponent<PedestrianDecisionControl>().direction.transform.position);
        }

        // Draw first line from this transform to the target
        Gizmos.color = Color.white;
        Gizmos.DrawLine(this.transform.position, agentRoute.activeRoute[0].transform.position);

        // Draw the path
        for (int i = 0; i < agentRoute.activeRoute.Length - 1; i++)
            Gizmos.DrawLine(agentRoute.activeRoute[i].transform.position, agentRoute.activeRoute[i + 1].transform.position);
    }
    // Start is called before the first frame update

    void Start()
    {
        // Get agentRoute from AICharacterBehaviour script
        agentRoute = this.GetComponent<PedestrianDecisionControl>().agentRoute;
        noOfRouteNodes = agentRoute.activeRoute.Length - 2; // Exclude endNode & startNode

        AgentReset();
    }

    public override void CollectObservations()
    {
        AddVectorObs(-(this.transform.localPosition.x - 5) / 10f);
        AddVectorObs(-(endNode.transform.localPosition.x - 5) / 10f);

        AddVectorObs(false);
        AddVectorObs(-1f);
        AddVectorObs(-1f);

        AddVectorObs(0f); AddVectorObs(0f);
    }

    public override void AgentReset()
    {
        agentRoute.activeRoute[0].gameObject.transform.position = this.transform.position;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // Set position for each route node
        for (int i = 0; i < vectorAction.Length; i++)
        {
            Vector3 nodePos = new Vector3(vectorAction[i] * 5, 0.5f, RouteNode(i + 1).z);
            agentRoute.activeRoute[i + 1].gameObject.transform.localPosition = nodePos;
        }
    }

    private Vector3 RouteNode(int i) { return agentRoute.activeRoute[i].gameObject.transform.localPosition; }

    // Update is called once per frame
    void Update()
    {
        
    }
}
