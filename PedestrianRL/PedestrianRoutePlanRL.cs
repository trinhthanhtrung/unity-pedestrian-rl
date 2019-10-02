using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityStandardAssets.CrossPlatformInput;

/// <summary>
/// This class is for reinforcement learning on a AI Third Person Controller prefab
/// This requires an agent route for navigation, and
/// an current target Game Object for controlling the direction.
/// Be sure to place the agent on a NavMesh area.
/// </summary>
public class PedestrianRoutePlanRL : Agent
{
    public PedestrianEnd endNode;
    public Transform ground;
    public GameObject[] obstacles; // List of all obstacles

    private AgentRoute agentRoute;
    private int noOfRouteNodes;

    private Vector3 startingPosition;
    private const float RESET_BOUNDARY_LIMIT = 25f;
    private const float FOLLOW_MULTIPLIER = 1f;

    const int MAX_ENV_STEPS = 300;
    private int resetCounter;
    private bool resetEnv; // Retrain the episode without resetting
    private bool _justCalledDone; 

    // Curriculum settings
    Academy routeplanAcademy;
    int noOfObstacles;
    int randomisedObsPos;

    // Data to be debugged
    float shortestRouteRwd = 0;
    float changingDirRwd = 0;
    float walkingParallelRwd = 0;
    float lawObeyanceRwd = 0;
    float obstacleRwd = 0;

    void OnDrawGizmosSelected()
    {
        // Draw a the route path when selected in Unity
        agentRoute = this.GetComponent<PedestrianRouteControl>().agentRoute;
        // Draws first line from this transform to the target
        Gizmos.color = Color.white;
        Gizmos.DrawLine(this.transform.position, agentRoute.activeRoute[0].transform.position);
        // Draw the path
        for (int i = 0; i < agentRoute.activeRoute.Length - 1; i++)
            Gizmos.DrawLine(agentRoute.activeRoute[i].transform.position, agentRoute.activeRoute[i + 1].transform.position);
    }

    // Start is called before the first frame update
    void Start()
    {
        routeplanAcademy = FindObjectOfType<AcademyPedestrian>();
        noOfObstacles = (int)routeplanAcademy.resetParameters["noOfObstacles"];
        randomisedObsPos = (int)routeplanAcademy.resetParameters["randomisedObsPos"];

        // Get agentRoute from AICharacterBehaviour script
        agentRoute = this.GetComponent<PedestrianRouteControl>().agentRoute;
        noOfRouteNodes = agentRoute.activeRoute.Length - 2; // Exclude endNode & startNode
        startingPosition = this.transform.localPosition;

        resetEnv = true;
        _justCalledDone = false;

        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].GetComponent<Obstacle>().isActive = false;
            foreach (Transform child in obstacles[i].transform)
                child.GetComponent<Renderer>().enabled = false;
        }

        AgentReset();
    }

    public override void CollectObservations()
    {
        AddVectorObs((this.transform.localPosition.x - ground.transform.localPosition.x + 5) / 10f);
        AddVectorObs((endNode.transform.localPosition.x - ground.transform.localPosition.x + 5) / 10f);
        if (obstacles[0].GetComponent<Obstacle>().isActive)
        {
            AddVectorObs(true);

            AddVectorObs((obstacles[0].transform.localPosition.x - ground.transform.localPosition.x + 5) / 10f);
            AddVectorObs((obstacles[0].transform.localPosition.z - ground.transform.localPosition.z + 5) / 10f);

            AddVectorObs(obstacles[0].GetComponent<Obstacle>().size);
            AddVectorObs(obstacles[0].GetComponent<Obstacle>().dangerLevel);
        }
        else
        {
            AddVectorObs(false);
            AddVectorObs(-1f);
            AddVectorObs(-1f);

            AddVectorObs(0f); AddVectorObs(0f);
        }
    }

    public override void AgentReset()
    {
        resetCounter++;

        changingDirRwd = shortestRouteRwd = lawObeyanceRwd = walkingParallelRwd = obstacleRwd = 0;

        if (resetEnv || resetCounter > MAX_ENV_STEPS)
        {
            if (resetCounter > MAX_ENV_STEPS) resetCounter = 0;

            // Get number of obstacles for curriculum learning
            noOfObstacles = (int)routeplanAcademy.resetParameters["noOfObstacles"];
            randomisedObsPos = (int)routeplanAcademy.resetParameters["randomisedObsPos"];

            // Reset to original position
            this.transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, -12f);
            this.endNode.transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, 10f);
            startingPosition = this.transform.localPosition;
            agentRoute.activeRoute[0].gameObject.transform.localPosition = startingPosition;

            // Randomise obstacle position
            for (int i = 0; i < noOfObstacles; i++)
            {
                //// Randomise enability of obstacles
                if (randomisedObsPos == 0)
                    obstacles[i].GetComponent<Obstacle>().isActive = true;
                else
                    obstacles[i].GetComponent<Obstacle>().isActive = Random.value > 0.5f;

                if (obstacles[i].GetComponent<Obstacle>().isActive)
                {
                    // Displayed on scene
                    foreach (Transform child in obstacles[i].transform)
                        child.GetComponent<Renderer>().enabled = true;

                    // Randomise position
                    obstacles[i].transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-10f, 8f));

                    // Randomise size
                    float obstacleSize = Random.Range(0.5f, 2f);
                    obstacles[i].GetComponent<Obstacle>().size = obstacleSize;
                    obstacles[i].transform.localScale = new Vector3(obstacleSize, obstacleSize, obstacleSize);
                    // Randomise danger level
                    obstacles[i].GetComponent<Obstacle>().dangerLevel = Random.Range(0f, 1f);

                }
                else
                {

                    // Hidden on scene
                    foreach (Transform child in obstacles[i].transform)
                        child.GetComponent<Renderer>().enabled = false;
                }
            }

        }

    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (_justCalledDone)
        {
            _justCalledDone = false;
        }
        else
        {
            resetEnv = true;

            // Set position for each route node
            for (int i = 0; i < vectorAction.Length; i++)
            {
                Vector3 nodePos = new Vector3(vectorAction[i] * 5, 0.5f, RouteNode(i + 1).z);
                agentRoute.activeRoute[i + 1].gameObject.transform.localPosition = nodePos;
            }

            // REWARDS
            // Shortest route possible
            AddReward((SumSqrPathLength() - 40f) * -0.004f);
            shortestRouteRwd += (SumSqrPathLength() - 40f) * -0.004f;

            // Least direction changes possible 
            for (int i = 0; i < noOfRouteNodes; i++)
            {
                Vector2 prevRouteAngle, nextRouteAngle;
                prevRouteAngle = new Vector2(RouteNode(i + 1).x - RouteNode(i).x,
                                             RouteNode(i + 1).z - RouteNode(i).z);
                nextRouteAngle = new Vector2(RouteNode(i + 2).x - RouteNode(i + 1).x,
                                             RouteNode(i + 2).z - RouteNode(i + 1).z);
                // Only put a penalty to the agent if the change in direction is > 30*
                if (Mathf.Abs(Vector2.Angle(prevRouteAngle, nextRouteAngle)) > 30f)
                {
                    AddReward(-0.03f);
                    changingDirRwd += -0.03f;
                }
            }
            // Calculate the position on the road in order to apply various rewards
            for (int i = 0; i < noOfRouteNodes + 1; i++)
            {
                int NUMBER_OF_SAMPLES = 20; // The higher the number, the more positions are added to rewards
                float xInterpolant = 1f / (float)NUMBER_OF_SAMPLES;

                // Favour walking parallel to the boundary
                AddReward(Mathf.Abs(RouteNode(i + 1).x - RouteNode(i).x) * -0.01f);
                walkingParallelRwd += Mathf.Abs(RouteNode(i + 1).x - RouteNode(i).x) * -0.01f;


                for (int j = 0; j < NUMBER_OF_SAMPLES; j++)
                {
                    Vector2 prevNode = new Vector2(RouteNode(i).x, RouteNode(i).z);
                    Vector2 currentNode = new Vector2(RouteNode(i + 1).x, RouteNode(i + 1).z);
                    // Get current x position of the route for each iteration
                    Vector2 simulatedPos = Vector2.Lerp(prevNode, currentNode, j * xInterpolant);

                    AddReward(XPosReward(simulatedPos.x));
                    AddReward(ObstacleReward(simulatedPos));
                }
            }

            Done();
            _justCalledDone = true;

        }
    }

    private Vector3 RouteNode(int i) { return agentRoute.activeRoute[i].gameObject.transform.localPosition; }

    private float SumSqrPathLength()
    {
        float sqrPathLength = 0;

        for (int i = 0; i < noOfRouteNodes + 1; i++)
        {
            Vector2 pathDist; // Distance between two nodes
            pathDist = new Vector2(RouteNode(i + 1).x - RouteNode(i).x,
                                   RouteNode(i + 1).z - RouteNode(i).z);
            sqrPathLength += Vector2.SqrMagnitude(pathDist);
        }

        return sqrPathLength;
    }

    // Add reward based on current x position of the agent
    private float XPosReward(float xPosition)
    {
        float totalReward = 0f;
        // Favor walking on the left
        if (xPosition < 0)
            totalReward += 2f;
        else
            totalReward -= 2f;

        // Avoid being too close to the boundary
        if (xPosition > 4.5f || xPosition < -4.5f)
            totalReward -= 1f;

        lawObeyanceRwd += totalReward * 0.0005f;

        return totalReward * 0.0005f;
    }

    // Add reward based on distance to obstacle(s)
    private float ObstacleReward(Vector2 currentPosition)
    {
        float totalReward = 0f;

        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i].GetComponent<Obstacle>().isActive)
            {
                Vector2 obstaclePos = new Vector2(obstacles[i].transform.localPosition.x, obstacles[i].transform.localPosition.z);
                float distCoef = Vector2.SqrMagnitude(obstaclePos - currentPosition) - obstacles[i].GetComponent<Obstacle>().size * obstacles[i].GetComponent<Obstacle>().size;
                if (distCoef < 0) // Route conflict
                {
                    resetEnv = false;
                    totalReward += distCoef / (obstacles[i].GetComponent<Obstacle>().size * obstacles[i].GetComponent<Obstacle>().size) * 
                                               Mathf.Pow(obstacles[i].GetComponent<Obstacle>().dangerLevel, 2);
                    Done();
                }
                else
                {
                    // Add a small bonus if there're obstacle(s) and you're not touching them
                    totalReward += 0.01f * Mathf.Pow(obstacles[i].GetComponent<Obstacle>().dangerLevel, 2);
                }
            }
        }

        obstacleRwd += totalReward * 0.1f;
        
        return totalReward * 0.1f;
    }

}
