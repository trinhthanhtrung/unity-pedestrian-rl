using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public enum ControlType
{
    Manual,
    Auto
}

public class CharacterDirectionControl : MonoBehaviour
{
    public ControlType controlType = ControlType.Auto;

    public Transform target;
    public float speed;
    public float size = 1f;
    public float dangerLevel = 1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        Vector3 direction2D = new Vector3();

        // Fix velocity ranged from [0, 1]
        if (speed < 0) speed = 0;
        else if (speed > 1) speed = 1;

        if (controlType == ControlType.Auto)
        {
            Vector3 direction = target.position - this.transform.position;
            direction2D.x = direction.normalized.x;
            direction2D.z = direction.normalized.z;
        }
        else if (controlType == ControlType.Manual)
        {
            // IF USER INTERACTS
            // Read input from users
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            speed = Mathf.Sqrt((horizontalInput * horizontalInput + verticalInput * verticalInput) / 2f);
            direction2D.x = horizontalInput;
            direction2D.z = verticalInput;

            target.transform.position = this.transform.position + new Vector3(horizontalInput, 0, verticalInput);
        }

        if (!this.GetComponent<NavMeshAgent>() && !this.GetComponent<AICharacterControl>())
            this.transform.position += direction2D * (3 * Time.deltaTime) * speed;
        else
        {
            this.GetComponent<NavMeshAgent>().speed = speed;
            this.GetComponent<AICharacterControl>().target = target;
        }
    }

}
