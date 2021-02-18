using MLAgents;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public enum ObstaclePredictionMode
{
    None,
    Forward,
    MovementPrediction
}

/// <summary>
/// This class is specificly for RL training for interacting task of
/// pedestrian agent. 
/// </summary>
public class PedestrianInteractingRL : Agent
{
    public Transform currentDestination;
    public Transform Direction;

    private GameObject pedestrianObstacle;
    private float defaultSpeed;
    private float pathLength;
    private bool hasPedestrianObs;
    private float prevDistance;
    private float prevAngle;
    private float prevSpeed;
    private int resetCount;

    // Learning mode using radial coordinate
    // If using radial mode, agent will use the relative angles and
    // distances to destination and obstacle. Output will be the 
    // changes in angle and speed.
    // If not, agent will use relative (x,y) position of destination 
    // and obstacle. Output will be the velocity vector.
    const bool RADIAL_MODE = true;

    // If usingNavMeshAgent is used, the script will reference to CharacterDirectionControl
    // instead of AI Character Control's components. If usingNavMeshAgent is not used, only 
    // Character Direction Control is required.
    public bool usingNavMeshAgent;

    public ObstaclePredictionMode obstaclePredictionMode = ObstaclePredictionMode.None;
    public float obsForwardDistanceGamma = 4f;
    //Vector3 obsForwardPosition;
    public GameObject predictObsPrefab;
    private GameObject predictedObsObject;

    private GameObject evaluator;

    // Reward variables
    float N_goal = 0;
    float N_behaviour = 0;

    // Rewarding coefficients
    const float OBS_COEF = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        if (!usingNavMeshAgent)
        {
            if (this.Direction)
                this.GetComponent<CharacterDirectionControl>().target = this.Direction;
            else
                Debug.LogError("A direction game object is required.");

            defaultSpeed = this.GetComponent<CharacterDirectionControl>().speed;
        }
        else
        {
            if (this.Direction)
                this.GetComponent<AICharacterControl>().target = this.Direction;
            else
                Debug.LogError("A direction game object is required.");

            defaultSpeed = this.GetComponent<NavMeshAgent>().speed;
        }

        if (obstaclePredictionMode == ObstaclePredictionMode.None)
            obsForwardDistanceGamma = 0;
        else if (obstaclePredictionMode == ObstaclePredictionMode.MovementPrediction)
        {
            if (!this.GetComponent<MovementPreditor>())
                Debug.LogError("A MovementPredictor component is required for Movement Prediction in Obstacle Prediction Mode to work!");
        }

        if (predictObsPrefab)
            predictedObsObject = Instantiate(predictObsPrefab, this.transform.position, Quaternion.identity);
        else
            predictedObsObject = new GameObject("Predicted Position of obstacle.");
        predictedObsObject.transform.parent = this.transform;

        pathLength = Vector3.SqrMagnitude(this.transform.position - currentDestination.transform.position);
        hasPedestrianObs = false;
        prevDistance = 100f;

        evaluator = GameObject.FindGameObjectWithTag("Evaluator");

        AgentReset();
    }

    public override void CollectObservations()
    {
        float obsSpeed;
        Transform obsTarget;
        float obsSize;
        float obsDanger;

        // Relative position of the current destination
        Vector3 currentDestRelative = currentDestination.position - this.transform.position;

        if (RADIAL_MODE)
        {
            AddVectorObs(currentDestRelative.magnitude / 5f);
            AddVectorObs(Vector3.SignedAngle(currentDestRelative, Direction.position - this.transform.position, Vector3.up) / 180f);
        }
        else
        {
            AddVectorObs(currentDestRelative.x / 5f);
            AddVectorObs(currentDestRelative.z / 5f);
        }


        // Observe current state of pedestrian obstacle
        if (pedestrianObstacle)
        {
            if (!usingNavMeshAgent)
            {
                obsSpeed = pedestrianObstacle.GetComponent<CharacterDirectionControl>().speed;
                obsTarget = pedestrianObstacle.GetComponent<CharacterDirectionControl>().target;
                obsSize = pedestrianObstacle.GetComponent<CharacterDirectionControl>().size;
                obsDanger = pedestrianObstacle.GetComponent<CharacterDirectionControl>().dangerLevel;
            }
            else
            {
                obsSpeed = pedestrianObstacle.GetComponent<NavMeshAgent>().speed;
                obsTarget = pedestrianObstacle.GetComponent<AICharacterControl>().target;
                obsSize = pedestrianObstacle.GetComponent<Obstacle>().size;
                obsDanger = pedestrianObstacle.GetComponent<Obstacle>().dangerLevel;
            }

            // Get predicted future obs position
            if (obstaclePredictionMode != ObstaclePredictionMode.MovementPrediction)
            {
                Vector3 obsDirection = obsTarget.transform.position - pedestrianObstacle.transform.position;
                obsDirection.Normalize();
                predictedObsObject.transform.position = pedestrianObstacle.transform.position + obsDirection
                                    * obsSpeed * obsForwardDistanceGamma;
            }
            else
            {
                predictedObsObject.transform.position = this.GetComponent<MovementPreditor>().GetPredictedTargetPosition();

                // Lower confident rate could heighten the 'feel' of danger
                float confidence = this.GetComponent<MovementPreditor>().GetConfidentRate();
                // Add to obsDanger if confident rate is low. If confident rate is 0, obsDanger is increased to 1
                // If confident rate is 1, no change to the obsDanger at all
                obsDanger += (1f - obsDanger) * (1f - confidence);
            }
        }
        else
        {
            predictedObsObject.transform.position = new Vector3(10f, 10f, 10f);
            obsSpeed = 0;
            obsTarget = null;
            obsSize = 0;
            obsDanger = 0;
        }

        if (pedestrianObstacle)
        {
            AddVectorObs(1); // Has obstacle

            if (RADIAL_MODE)
            {
                Vector3 directionToObsForwardPos = predictedObsObject.transform.position - this.transform.position;

                AddVectorObs(directionToObsForwardPos.magnitude / 15f);
                AddVectorObs(Vector3.SignedAngle(directionToObsForwardPos, Direction.position - this.transform.position, Vector3.up) / 180f);

                // Obstacle speed and angle
                AddVectorObs(obsSpeed);
                AddVectorObs(Vector3.SignedAngle(directionToObsForwardPos, obsTarget.position - predictedObsObject.transform.position, 
                             Vector3.up) / 180f); // Angle formed by obs's direction and obs's vector toward agent.

                // Obstacle size and danger level
                AddVectorObs(obsSize);
                AddVectorObs(obsDanger);
            }
            else
            {
                AddVectorObs((predictedObsObject.transform.position.x - this.transform.position.x) / 10f);
                AddVectorObs((predictedObsObject.transform.position.z - this.transform.position.z) / 10f);

                AddVectorObs((obsTarget.position.x - pedestrianObstacle.transform.position.x) / 10f);
                AddVectorObs((obsTarget.position.z - pedestrianObstacle.transform.position.z) / 10f);

                AddVectorObs(obsSize);
                AddVectorObs(obsDanger);
            }

        }
        else
        {
            AddVectorObs(0); AddVectorObs(-1f); AddVectorObs(-1f); AddVectorObs(-1); AddVectorObs(-1); AddVectorObs(0); AddVectorObs(0);
            if (predictedObsObject) predictedObsObject.transform.position = new Vector3(10f, 10f, 10f);
        }
    }

    public override void AgentReset()
    {
        N_goal = 0;
        N_behaviour = 0;

        resetCount = 500;
        prevDistance = 100f;

        // Reset agent's attributes
        if (!usingNavMeshAgent)
            this.GetComponent<CharacterDirectionControl>().speed = defaultSpeed;
        else
            this.GetComponent<NavMeshAgent>().speed = defaultSpeed;

        prevSpeed = defaultSpeed;

        if (!evaluator) /// TRAINING
        {
            this.transform.position = new Vector3(0, 0.8f, 0);

            // Randomise destination's position 
            float destAngleR = Random.Range(-180f, 180f) * Mathf.Deg2Rad;
            prevAngle = destAngleR;

            pathLength = 5f;
            //pathLength = Random.Range(2f, 4.5f);
            currentDestination.transform.localPosition =
                new Vector3(pathLength * Mathf.Cos(destAngleR), 0.5f, pathLength * Mathf.Sin(destAngleR));
        }
        else /// VALIDATING
            evaluator.GetComponent<Evaluator>().InitNewEnvironment(this.gameObject, currentDestination.gameObject);


        this.Direction.transform.position = this.currentDestination.transform.position;

        if (pedestrianObstacle)
        {
            hasPedestrianObs = true;
            if (!usingNavMeshAgent)
            {
                pedestrianObstacle.GetComponent<CharacterDirectionControl>().size = Random.Range(0.5f, 2f);
                pedestrianObstacle.GetComponent<CharacterDirectionControl>().dangerLevel = Random.Range(0f, 1f);
            }
            else
            {
                pedestrianObstacle.GetComponent<Obstacle>().size = Random.Range(0.5f, 2f);
                pedestrianObstacle.GetComponent<Obstacle>().dangerLevel = Random.Range(0f, 1f);
            }
        }
        else
            hasPedestrianObs = false;

    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        resetCount--;

        //Monitor.Log("Reward: ", GetCumulativeReward().ToString());

        Vector3 currentDirection = this.Direction.transform.position - this.transform.position;
        currentDirection.Normalize();
        float currentSpeed = prevSpeed;

        if (RADIAL_MODE)
        {
            // VectorAction assigned to change in speed and angle
            float angleChange = vectorAction[1] * 10f;
            // Set new direction 
            currentDirection = Quaternion.AngleAxis(angleChange, Vector3.up) * currentDirection;
            this.Direction.transform.position = this.transform.position + currentDirection;
            // Set new speed
            currentSpeed = (vectorAction[0] + 1f) / 2f;
        }
        else
        {
            Vector2 directionVector = new Vector2(vectorAction[0], vectorAction[1]);

            // Set new speed
            currentSpeed = directionVector.magnitude / Mathf.Sqrt(2f);

            directionVector.Normalize();
            this.Direction.transform.localPosition = new Vector3(directionVector.x, this.transform.position.y, directionVector.y);
            currentDirection = this.Direction.transform.position - this.transform.position;
        }

        // Get current angle
        float angle = Vector2.SignedAngle(new Vector2(this.Direction.transform.localPosition.x,
                                                      this.Direction.transform.localPosition.z), Vector2.right) * Mathf.Deg2Rad;
        // Set current speed to character control
        if (!usingNavMeshAgent)
            this.GetComponent<CharacterDirectionControl>().speed = currentSpeed;
        else
            this.GetComponent<NavMeshAgent>().speed = currentSpeed;

        //!REWARD
        /// Discount for behaviour of interacting with obstacle 
        /// When closer to obstacle, the agent would concentrate on behaving nicely, such as avoiding (more will be added).
        /// When not being close to the obstacle (small gamma), the agent would concentrate on performing goal-centered 
        /// actions, such as keeping intended speed and walking straight to the destination.
        float gamma = 0;

        // Encourage the agent if it make closer to the destination
        float currentDistance = Vector3.SqrMagnitude(this.transform.position - currentDestination.transform.position);
        if (currentDistance < prevDistance)
            N_goal += (prevDistance - currentDistance) / pathLength * 0.01f;

        // Discourage angle and speed change
        N_goal += -(currentSpeed - defaultSpeed) * (currentSpeed - defaultSpeed) * 0.3f;
        if (Mathf.Abs(angle - prevAngle) > 30 * Mathf.Deg2Rad)
            N_goal += -0.02f;

        prevDistance = currentDistance;
        prevSpeed = currentSpeed;
        prevAngle = angle;

        if (pedestrianObstacle)
        {
            float obsSize = 1f;
            float obsDanger = 1f;
            float SQR_MIN_DIST = 1;

            if (!usingNavMeshAgent)
            {
                obsSize = pedestrianObstacle.GetComponent<CharacterDirectionControl>().size;
                obsDanger = pedestrianObstacle.GetComponent<CharacterDirectionControl>().dangerLevel;
            }
            else
            {
                obsSize = pedestrianObstacle.GetComponent<Obstacle>().size;
                obsDanger = pedestrianObstacle.GetComponent<Obstacle>().dangerLevel;
            }

            float sqrDistToObs = Vector3.SqrMagnitude(predictedObsObject.transform.position - this.transform.position);
            gamma = 1 / (Mathf.Sqrt(sqrDistToObs) * obsSize + 1);

            if (sqrDistToObs * obsSize < SQR_MIN_DIST) // Collide with obstacle
            {
                N_behaviour += -OBS_COEF * obsDanger;
            }
            else if (sqrDistToObs < (SQR_MIN_DIST + 1.2f)) // Close to collision w/ obs
            {
                N_behaviour += (sqrDistToObs - SQR_MIN_DIST) * 0.07f / 1.2f - 0.07f;
            }
            else 
            {
                //TODO: Need refactoring
                switch (obstaclePredictionMode)
                {
                    case ObstaclePredictionMode.None:
                        if (IsInFrontOf(pedestrianObstacle.transform, this.transform, 60f, SQR_MIN_DIST + 4.0f))
                            N_behaviour += ((sqrDistToObs - SQR_MIN_DIST) * OBS_COEF / 4.0f - OBS_COEF) * obsDanger; 
                        break;
                    case ObstaclePredictionMode.Forward:
                    case ObstaclePredictionMode.MovementPrediction:
                        if (IsCloseTo(predictedObsObject.transform, this.transform, SQR_MIN_DIST + 4.0f))
                            N_behaviour += ((sqrDistToObs - SQR_MIN_DIST) * OBS_COEF / 4.0f - OBS_COEF) * obsDanger;
                        break;
                }
                    
            }

        }
        else
            gamma = 0;

        float totalReward = (1 - gamma) * N_goal + gamma * N_behaviour;

        AddReward(totalReward);
        N_goal = 0;
        N_behaviour = 0;

        // Reset the scene when reaching destination
        if (IsCloseTo(this.currentDestination.transform, this.transform, 0.2f))
        {
            AddReward(0.5f);
            if (evaluator)
            {
                if (evaluator.GetComponent<Evaluator>().AllDone())
                    Done();
                else
                    evaluator.GetComponent<Evaluator>().SetEnvState(1, true);
            }
            else
                Done();
        }

        // Reset the scene when it takes too long
        if (resetCount <= 0)
        {
            AddReward(-1f);
            if (evaluator)
            {
                if (evaluator.GetComponent<Evaluator>().AllDone())
                    Done();
                else
                    evaluator.GetComponent<Evaluator>().SetEnvState(1, true);
            }
            else
                Done();
        }

    }
    // Update is called once per frame
    void Update()
    {
        GameObject nearestPedestrian = GameObject.FindGameObjectWithTag("Pedestrian");

        if (!nearestPedestrian) pedestrianObstacle = null;
        else
        {
            if (IsInFrontOf(this.transform, nearestPedestrian.transform))
                pedestrianObstacle = nearestPedestrian;
            else // Pedestrian is not in front of agent
                pedestrianObstacle = null;
        }
    }
    /// <summary>
    /// Check if one object is in front of another
    /// </summary>
    /// <returns>Return true if object2 is in front of object1.</returns>
    private bool IsInFrontOf(Transform object1, Transform object2)
    {
        return IsInFrontOf(object1, object2, 80f);
    }

    /// <summary>
    /// Check if one object is in front of another
    /// </summary>
    /// <returns>Return true if object2 is in front of a fixed angle (in degree) from object1.</returns>
    private bool IsInFrontOf(Transform object1, Transform object2, float angle)
    {
        Vector3 heading = object2.position - object1.position;

        // Get the direction of object 1
        Vector3 directionPosition;

        if (object1.GetComponent<PedestrianInteractingRL>())
            directionPosition = this.GetComponent<PedestrianInteractingRL>().Direction.position;
        else if (object1.GetComponent<AICharacterControl>())
            directionPosition = this.GetComponent<AICharacterControl>().target.position;
        else if (object1.GetComponent<CharacterDirectionControl>())
            directionPosition = this.GetComponent<CharacterDirectionControl>().target.position;
        else
        {
            float dot = Vector3.Dot(heading, this.transform.forward);
            if (dot < 0.1f) return false;
            else return true;
        }

        float headingAngle = Vector3.Angle(heading, directionPosition - object1.transform.position);
        if (Mathf.Abs(headingAngle) > angle)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Check if one object is in front of another within a distance
    /// </summary>
    /// <returns>Return true if object2 is in front of a fixed angle (in degree) 
    /// from object1 within the distance value.</returns>
    private bool IsInFrontOf(Transform object1, Transform object2, float angle, float sqrDist)
    {
        if (!IsCloseTo(object1, object2, sqrDist))
            return false;
        else
            return IsInFrontOf(object1, object2, angle);
    }

    private bool IsCloseTo(Transform object1, Transform object2, float sqrDist)
    {
        Vector2 obj1Pos = new Vector2(object1.position.x, object1.position.z);
        Vector2 obj2Pos = new Vector2(object2.position.x, object2.position.z);
        if (Vector2.SqrMagnitude(obj1Pos - obj2Pos) > sqrDist) 
            return false;
        else
            return true;
    }

    IEnumerator Wait2s()
    {
        yield return new WaitForSeconds(2);
    }
}
