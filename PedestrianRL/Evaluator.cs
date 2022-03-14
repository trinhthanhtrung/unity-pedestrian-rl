using System.IO;
using UnityEngine;

public class Evaluator : MonoBehaviour
{
    public GameObject agent;
    public GameObject obstacle;
    public GameObject agentNodePrefab;
    public GameObject obstacleNodePrefab;

    public GameObject SFMAgent;
    public GameObject SFMDestination;
    public GameObject SFMObstacle; // Mimic from obstacle

    public GameObject NavAgent;
    public GameObject NavDestination;
    public GameObject NavObstacle; // Mimic from obstacle

    public GameObject SimAgent;
    public GameObject SimObstacle; // Mimic from obstacle

    public bool showPath;

    // Paths
    private GameObject AgentPath, ObstaclePath;
    private GameObject SFMObstaclePath, SFMAgentPath;
    private GameObject NavAgentPath, NavObstaclePath;
    private GameObject SimAgentPath, SimObstaclePath;

    // Navigation Stats
    private float agentLength, SFMAgentLength, NavAgentLength;
    private float agentTime, SFMAgentTime, NavAgentTime;
    private float agentCollisions, SFMAgentCollisions, NavAgentCollisions;

    private Vector3 prevAgentPosition, prevSFMAgentPosition, prevNavAgentPosition;

    public int frameSkipping = 10;
    private int frameCount;

    // State of each environment
    private bool envRLDone;
    private bool envSFMDone;
    private bool envUnityNavDone;
    private bool envSimulatedDone;
    private bool usrManualDone;

    //private float prevTime;

    // Start is called before the first frame update
    void Start()
    {
        if (showPath)
        {
            if (agent)        AgentPath = InitPath(AgentPath, "Agent Path");
            if (obstacle)     ObstaclePath = InitPath(ObstaclePath, "Obstacle Path");
            if (SFMAgent)     SFMAgentPath = InitPath(SFMAgentPath, "SFM Agent Path");
            if (SFMObstacle)  SFMObstaclePath = InitPath(SFMObstaclePath, "SFM Obstacle Path");
            if (NavAgent)     NavAgentPath = InitPath(NavAgentPath, "Nav Agent Path");
            if (NavObstacle)  NavObstaclePath = InitPath(NavObstaclePath, "Nav Obstacle Path");
            if (SimAgent)     SimAgentPath = InitPath(SimAgentPath, "Simulated Agent Path");
            if (SimObstacle)  SimObstaclePath = InitPath(SimObstaclePath, "Simulated Obstacle Path");
        }

        if (!agent)
            agent = this.gameObject;
        if (!obstacle)
            obstacle = GameObject.FindGameObjectWithTag("Pedestrian");
        frameCount = 0;

        usrManualDone = true;
        envRLDone = true;
        envSFMDone = true;
        envUnityNavDone = true;
        envSimulatedDone = true;

    }

    void FixedUpdate()
    {
        frameCount++;
        if (frameCount > frameSkipping)
        {
            // Create new position prefab and assign to the obstacle / agent path
            if (showPath)
            {
                if (AgentPath) 
                    Instantiate(agentNodePrefab, agent.transform.position, Quaternion.identity, AgentPath.transform);
                if (ObstaclePath)
                    Instantiate(obstacleNodePrefab, obstacle.transform.position, Quaternion.identity, ObstaclePath.transform);
                if (SFMAgentPath)
                    Instantiate(agentNodePrefab, SFMAgent.transform.position, Quaternion.identity, SFMAgentPath.transform);
                if (SFMObstaclePath)
                    Instantiate(obstacleNodePrefab, SFMObstacle.transform.position, Quaternion.identity, SFMObstaclePath.transform);
                if (NavAgentPath)
                    Instantiate(agentNodePrefab, NavAgent.transform.position, Quaternion.identity, NavAgentPath.transform);
                if (NavObstaclePath)
                    Instantiate(obstacleNodePrefab, NavObstacle.transform.position, Quaternion.identity, NavObstaclePath.transform);
                if (SimAgentPath)
                    Instantiate(agentNodePrefab, SimAgent.transform.position, Quaternion.identity, SimAgentPath.transform);
                if (SimObstaclePath)
                    Instantiate(obstacleNodePrefab, SimObstacle.transform.position, Quaternion.identity, SimObstaclePath.transform);

            }

            // Check if envSFM and envNav Done (RL will send state directly to Evaluator)
            if (IsColliding(SFMAgent.transform.position, SFMDestination.transform.position))
                envSFMDone = true;
            if (IsColliding(NavAgent.transform.position, NavDestination.transform.position))
                envUnityNavDone = true;    

            LogStats();

            frameCount = 0;
        }
    }
    
    // Update is called once per frame
    void Update()
    {

        if (SimAgent && Input.GetKeyDown(KeyCode.P))
        {
            agent.transform.localPosition = SimAgent.GetComponent<MovementRecorder>().GetStartingPosition();
            agent.GetComponent<PedestrianInteractingRL>().currentDestination.transform.position =
                agent.transform.position + new Vector3(0, 0.8f, 10f);
            if (SFMAgent) SFMAgent.transform.position = agent.transform.position + new Vector3(10f, 0, 0);
            if (NavAgent) NavAgent.transform.position = agent.transform.position + new Vector3(20f, 0, 0);
        }

        // Clone obstacle and destination's position
        if (SFMObstacle)
        {
            SFMObstacle.transform.position = obstacle.transform.position + new Vector3(10f, 0, 0);
            SFMDestination.transform.position = agent.GetComponent<PedestrianInteractingRL>().currentDestination.transform.position
                                                + new Vector3(10f, 0, 0);
        }
        if (NavObstacle)
        {
            NavObstacle.transform.position = obstacle.transform.position + new Vector3(20f, 0, 0);
            NavDestination.transform.position = agent.GetComponent<PedestrianInteractingRL>().currentDestination.transform.position
                                                + new Vector3(20f, 0, 0);
        }
        if (SimObstacle)
        {
            SimObstacle.transform.position = obstacle.transform.position + new Vector3(-10f, 0, 0);
        }


        if (Input.GetKeyDown(KeyCode.Space))
            usrManualDone = true;


    }

    private void LogStats()
    {
        if (!envRLDone)
        {
            agentTime++;
            if (IsColliding(agent.transform.position, obstacle.transform.position, 2f))
                agentCollisions++;
            agentLength += Mathf.Sqrt(Vector3.SqrMagnitude(agent.transform.position - prevAgentPosition));
            prevAgentPosition = agent.transform.position;
        }
        if (!envSFMDone)
        {
            SFMAgentTime++;
            if (IsColliding(SFMAgent.transform.position, SFMObstacle.transform.position, 2f))
                SFMAgentCollisions++;
            SFMAgentLength += Mathf.Sqrt(Vector3.SqrMagnitude(SFMAgent.transform.position - prevSFMAgentPosition));
            prevSFMAgentPosition = SFMAgent.transform.position;
        }
        if (!envUnityNavDone)
        {
            NavAgentTime++;
            if (IsColliding(NavAgent.transform.position, NavObstacle.transform.position, 2f))
                NavAgentCollisions++;
            NavAgentLength += Mathf.Sqrt(Vector3.SqrMagnitude(NavAgent.transform.position - prevNavAgentPosition));
            prevNavAgentPosition = NavAgent.transform.position;
        }

    }

    /// <summary>
    /// Reset the evaluator
    /// </summary>
    public void InitNewEnvironment(GameObject agent, GameObject destination)
    {
        string path = "Assets/Resources/navlog.csv";

        //Write text to the CSV file
        StreamWriter writer = new StreamWriter(path, true);
        writer.Write(agentLength); writer.Write(",");
        writer.Write(agentTime); writer.Write(",");
        writer.Write(agentCollisions); writer.Write(",");
        writer.Write(SFMAgentLength); writer.Write(",");
        writer.Write(SFMAgentTime); writer.Write(",");
        writer.Write(SFMAgentCollisions); writer.Write(",");
        writer.Write(NavAgentLength); writer.Write(",");
        writer.Write(NavAgentTime); writer.Write(",");
        writer.Write(NavAgentCollisions); writer.WriteLine();
        writer.Close();

        if (showPath)
        {
            AgentPath = InitPath(AgentPath, "Agent Path");
            ObstaclePath = InitPath(ObstaclePath, "Obstacle Path");
            SFMAgentPath = InitPath(SFMAgentPath, "SFM Agent Path");
            SFMObstaclePath = InitPath(SFMObstaclePath, "SFM Obstacle Path");
            NavAgentPath = InitPath(NavAgentPath, "Nav Agent Path");
            NavObstaclePath = InitPath(NavObstaclePath, "Nav Obstacle Path");
        }

        // Set positions for validating
        agent.transform.position = new Vector3(Random.Range(-2.5f, 2.5f), 0.8f, -2.5f);
        destination.transform.position = new Vector3(Random.Range(-2.5f, 2.5f), 0.8f, 2.5f);

        // Copy position of obstacle to the evaluated environment
        SFMAgent.transform.position = agent.transform.position + new Vector3(10f, 0, 0);
        NavAgent.transform.position = agent.transform.position + new Vector3(20f, 0, 0);

        agentLength = SFMAgentLength = NavAgentLength = 0;
        agentTime = SFMAgentTime = NavAgentTime = 0;
        agentCollisions = SFMAgentCollisions = NavAgentCollisions = 0;
        prevAgentPosition = agent.transform.position;
        prevSFMAgentPosition = SFMAgent.transform.position;
        prevNavAgentPosition = NavAgent.transform.position;

        envRLDone = false;
        envSFMDone = false;
        envUnityNavDone = false;
        envSimulatedDone = false;
        usrManualDone = false;
    } 

    /// <summary>
    /// Destroy the path and create new one with pathName
    /// </summary>
    /// <param name="path">Path to reset</param>
    /// <param name="pathName">Name given to the new path</param>
    private GameObject InitPath(GameObject path, string pathName)
    {
        if (path)
            Destroy(path);
        path = new GameObject(pathName);
        path.transform.parent = this.transform;
        return path;
    }

    /// <summary>
    /// Set training state for each environment
    /// </summary>
    /// <param name="env">Environment ID (1: Behavioural RL; 2: Social Force Model; 3: Unity Nav; 0: User press</param>
    /// <param name="state"></param>
    public void SetEnvState(int env, bool state = true)
    {
        switch (env)
        {
            case 1: envRLDone = state; break;
            case 2: envSFMDone = state; break; 
            case 3: envUnityNavDone = state; break;
            case 4: envSimulatedDone = state; break;
            case 0: usrManualDone = state; break;
        }
    }
    
    /// <summary>
    /// Set starting positions in evaluation mode
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="destination"></param>
    public void SetStartingPositions(GameObject agent, GameObject destination)
    {

    }

    private bool IsColliding(Vector3 obj1, Vector3 obj2, float sqrCollisionDist = 1f)
    {
        return (Vector3.SqrMagnitude(obj1 - obj2) < sqrCollisionDist);
    }

    public bool AllDone()
    {
        return usrManualDone && envRLDone && envSFMDone && envUnityNavDone && envSimulatedDone;
    }

    void OnDrawGizmosSelected()
    { 
        if (!(AgentPath && ObstaclePath && SFMAgentPath && SFMObstaclePath 
            && NavAgentPath && NavObstaclePath && SimAgentPath && SimObstaclePath))
            return;

        DrawLine(ObstaclePath);
        DrawLine(AgentPath);
        DrawLine(SFMAgentPath);
        DrawLine(SFMObstaclePath);
        DrawLine(NavAgentPath);
        DrawLine(NavObstaclePath);
        DrawLine(SimAgentPath);
        DrawLine(SimObstaclePath);
    }

    void DrawLine(GameObject path)
    {
        Transform previousNode = null;
        Gizmos.color = Color.gray;
        foreach (Transform node in path.transform)
        {
            if (previousNode)
                Gizmos.DrawLine(previousNode.position, node.position);
            previousNode = node;
        }
    }
}
