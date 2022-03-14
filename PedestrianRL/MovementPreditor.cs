using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementPreditor : MonoBehaviour
{
    [Tooltip("Target to predict the movement. Leave blank if current Game Object is observed")]
    public GameObject target;
    private GameObject prevTarget;

    [Tooltip("Game Object Prefab to display the prediction (if needed)")]
    public GameObject predictedPrefab;
    private GameObject predictedObject;

    // Last 5 target object's positions
    private float x_t1, y_t1, x_t2, y_t2, x_t3, y_t3, x_t4, y_t4, x_t5, y_t5;
    // and time
    private float t1, t2, t3, t4, t5;
    // Current target's position
    private float x_t, y_t;
    // Count the number of time object's positions were read
    private int noPosRead;
    // A ring buffer to store all recent set of x, y, t
    private float[] xBuffer;
    private float[] yBuffer;
    private float[] tBuffer;
    private const int BUFFER_SIZE = 50000;
    private const float BUFFER_DELTA = 0.05f; // Duration between two elements
    private int bufferPtr = 0;

    private float confidentRate;
    const float CONFIDENT_GAMMA = 1.7f; // Discount for confident rate for choosing the right duration between samples
    const float CONFIDENT_EPSILON_DEPENDENCE = 0.45f; // Higher dependence means the confident rate quickly matches epsilon value
    private float epsilon; // Epsilon rate: Rate of misprediction 
    private float[] epsilonBatch;
    const float EPSILON_DISCOUNT = 1.1f; // Discount for epsilon


    private float prevTime;

    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Pedestrian");

        if (target)
        {
            // Create new predicted object
            if (predictedPrefab)
                predictedObject = Instantiate(predictedPrefab, this.transform.position, Quaternion.identity);
            else
                predictedObject = new GameObject("Movement Prediction of " + target.name);

            // Initialise ring buffer
            xBuffer = new float[BUFFER_SIZE];
            yBuffer = new float[BUFFER_SIZE];
            tBuffer = new float[BUFFER_SIZE];

            epsilonBatch = new float[10];

            UpdateBuffer(target);

            x_t1 = x_t2 = x_t3 = x_t4 = x_t5 = target.transform.position.x;
            y_t1 = y_t2 = y_t3 = y_t4 = y_t5 = target.transform.position.z;
            x_t = y_t = 0;
            t1 = t2 = t3 = t4 = t5 = Time.time;
            noPosRead = 0;
            confidentRate = 0.5f;
            epsilon = 1f;

            prevTime = Time.time;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float currentTime = Time.time;

        target = GameObject.FindGameObjectWithTag("Pedestrian");

        if (target) //TODO Check if that's the same target
        {
            // Record the set of time, x, y to the buffer
            if (currentTime > tBuffer[bufferPtr - 1] + BUFFER_DELTA)
            {
                UpdateBuffer(target);
            }

            else if (Time.time > prevTime + 0.1f)
            {
                prevTime = Time.time;

                // Get the set of 4 previous data from the buffer

                int step = (int)(confidentRate * CONFIDENT_GAMMA / BUFFER_DELTA); 
                // Set the minimum time gap between every samples at 0.3 second as human perception
                // should not perceive movement quicker than this.
                if (step < (0.3f / BUFFER_DELTA))
                    step = (int)(0.3f / BUFFER_DELTA);

                x_t1 = xBuffer[(bufferPtr + BUFFER_SIZE - 4 * step) % BUFFER_SIZE];
                x_t2 = xBuffer[(bufferPtr + BUFFER_SIZE - 3 * step) % BUFFER_SIZE];
                x_t3 = xBuffer[(bufferPtr + BUFFER_SIZE - 2 * step) % BUFFER_SIZE];
                x_t4 = xBuffer[(bufferPtr + BUFFER_SIZE - step) % BUFFER_SIZE];
                y_t1 = yBuffer[(bufferPtr + BUFFER_SIZE - 4 * step) % BUFFER_SIZE];
                y_t2 = yBuffer[(bufferPtr + BUFFER_SIZE - 3 * step) % BUFFER_SIZE];
                y_t3 = yBuffer[(bufferPtr + BUFFER_SIZE - 2 * step) % BUFFER_SIZE];
                y_t4 = yBuffer[(bufferPtr + BUFFER_SIZE - step) % BUFFER_SIZE];
                t1 = tBuffer[(bufferPtr + BUFFER_SIZE - 4 * step) % BUFFER_SIZE];
                t2 = tBuffer[(bufferPtr + BUFFER_SIZE - 3 * step) % BUFFER_SIZE];
                t3 = tBuffer[(bufferPtr + BUFFER_SIZE - 2 * step) % BUFFER_SIZE];
                t4 = tBuffer[(bufferPtr + BUFFER_SIZE - step) % BUFFER_SIZE];

                x_t5 = Mathf.Round(target.transform.position.x * 1000f) / 1000f;
                y_t5 = Mathf.Round(target.transform.position.z * 1000f) / 1000f;
                t5 = Time.time;

                // Increase the number of time object's positions were read
                // if there's not enough data.
                if (noPosRead < 5)
                    noPosRead++;

                if (target != prevTarget)
                {
                    noPosRead = 0;
                }
                else if (noPosRead == 5) // Same target and there are 5 points read
                {
                    /// Calculate the position of the predicted point (x_t, y_t) on if 
                    /// the Lagrange cubic curve formed by 4 set of (x, y, t)

                    x_t = P(t5, new float[] { t1, x_t1, t2, x_t2, t3, x_t3, t4, x_t4 });
                    y_t = P(t5, new float[] { t1, y_t1, t2, y_t2, t3, y_t3, t4, y_t4 });
 
                    float sqrPredictionOffset = (x_t - x_t5) * (x_t - x_t5) + (y_t - y_t5) * (y_t - y_t5);
                    float sqrPointsDistance = (x_t5 - x_t1) * (x_t5 - x_t1) + (y_t5 - y_t1) * (y_t5 - y_t1);

                    if (sqrPointsDistance != 0)
                    {
                        float scaledPointDistance = Mathf.Sqrt(sqrPredictionOffset / sqrPointsDistance);
                        epsilon = 1f / Mathf.Pow(scaledPointDistance + 1f, 5f);
                    }
                    else
                        epsilon = 1f; 

                    AddEpsilon(epsilon);

                    CalculateConfidence();

                    // Calculate the predicted position of the object using the x=x(t) and y=y(t) functions
                    // at timestep t + theta || theta = confident rate x epsilon x epsilon discount

                    // If previous prediction is correct, the predictable position of the object will be at EPSILON_DISCOUNT seconds in the future.
                    float x_p = P(t5 + confidentRate * epsilon * EPSILON_DISCOUNT, new float[] { t2, x_t2, t3, x_t3, t4, x_t4, t5, x_t5 });
                    float y_p = P(t5 + confidentRate * epsilon * EPSILON_DISCOUNT, new float[] { t2, y_t2, t3, y_t3, t4, y_t4, t5, y_t5 });

                    // Update predicted prefab's position
                    predictedObject.transform.position = new Vector3(x_p, target.transform.position.y, y_p);
                }

                // Set previous target to be later compared with current target
                prevTarget = target;
            }
        }
    }

    /// <summary>
    /// Update current target's position to x and y buffers. Also update current time to tBuffer.
    /// </summary>
    /// <param name="bufferPtr"></param>
    /// <param name="target"></param>
    private void UpdateBuffer(int bufferPtr, GameObject target)
    {
        xBuffer[bufferPtr] = Mathf.Round(target.transform.position.x * 1000f) / 1000f;
        yBuffer[bufferPtr] = Mathf.Round(target.transform.position.z * 1000f) / 1000f;
        tBuffer[bufferPtr] = Time.time;
    }

    /// <summary>
    /// Automatically update buffer to the latest element in the array and automatically increment bufferPtr.
    /// </summary>
    /// <param name="target"></param>
    private void UpdateBuffer(GameObject target)
    {
        if (bufferPtr >= BUFFER_SIZE)
            bufferPtr = 0;

        UpdateBuffer(bufferPtr, target);
        bufferPtr++;
    }

    /// Add epsilon into epsilonBatch    
    private void AddEpsilon(float epsilon)
    {
        for (int i = 0; i < 9; i++)
            epsilonBatch[i] = epsilonBatch[i+1];

        epsilonBatch[9] = epsilon;     
    }

    /// Calculate confident rate
    private void CalculateConfidence()
    {
        // Calculate confident rate based on epsilon
        //TODO Calculate confident rate based on the history of epsilon
        confidentRate += (epsilon - confidentRate) * CONFIDENT_EPSILON_DEPENDENCE;
    }    

    public Vector3 GetPredictedTargetPosition()
    {
        return predictedObject.transform.position;
    }

    public float GetConfidentRate()
    {
        return confidentRate;
    }

    /// <summary>
    /// Return the value of P(x) which P(x) is the curve defined by the four points on it
    /// </summary>
    /// <param name="x"></param>
    /// <param name="points">Array of 8 floats, consisting of x1, y1, x2, y2, x3, y3, x4, y4
    /// which are the coordinates for the 4 points which form the Lagrange cubic curve P(x)</param>
    /// <returns></returns>
    private float P(float x, float[] points)
    {
        /// Lagrange function for cubic curve passing 4 predefined points
        ///           (x-x2)(x-x3)(x-x4)            (x-x1)(x-x3)(x-x4)           (x-x1)(x-x2)(x-x4)            (x-x1)(x-x2)(x-x3)            
        ///  P(x):  ---------------------- . y1 + ---------------------- . y2 + ---------------------- . y3 + ---------------------- . y4  = 0
        ///         (x1-x2)(x1-x3)(x1-x4)          (x2-x1)(x2-x3)(x2-x4)         (x3-x1)(x3-x2)(x3-x4)        (x4-x1)(x4-x2)(x4-x3)       
        ///  or  A(x-x2)(x-x3)(x-x4) + B(x-x1)(x-x3)(x-x4) + C(x-x1)(x-x2)(x-x4) + D(x-x1)(x-x2)(x-x3)      

        float x1, y1, x2, y2, x3, y3, x4, y4;


        x1 = points[0]; y1 = points[1];
        x2 = points[2]; y2 = points[3];
        x3 = points[4]; y3 = points[5];
        x4 = points[6]; y4 = points[7];

        if ((x1 == x2) || (x1 == x3) || (x1 == x4) || (x2 == x3) || (x2 == x4) || (x3 == x4))
        {
            //Debug.LogWarning("Avoiding division by 0, although this should NOT happen.");
            //Debug.Log("Set of inputs: " + x1 + ", " + y1 + " || " + x2 + ", " + y2 + " || " + x3 + ", " + y3 + " || " + x4 + ", " + y4 + "\n");
            return 999f;
        }

        float lA, lB, lC, lD;
        lA = y1 / ((x1 - x2) * (x1 - x3) * (x1 - x4));
        lB = y2 / ((x2 - x1) * (x2 - x3) * (x2 - x4));
        lC = y3 / ((x3 - x1) * (x3 - x2) * (x3 - x4));
        lD = y4 / ((x4 - x1) * (x4 - x2) * (x4 - x3));

        float result = 0;
        result += lA * (x - x2) * (x - x3) * (x - x4);
        result += lB * (x - x1) * (x - x3) * (x - x4);
        result += lC * (x - x1) * (x - x2) * (x - x4);
        result += lD * (x - x1) * (x - x2) * (x - x3);

        return result;
    }

 
}
