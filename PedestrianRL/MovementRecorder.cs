using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public enum MovementMode
{
    Record, // Press R to start recording. Press R again to stop.
    ManualPlayback, // Press P to start movement. 
    AutoPlayback // Movement starts automatically.
}

public class MovementRecorder : MonoBehaviour
{
    public string path;
    StreamReader reader;

    public MovementMode movementMode = MovementMode.Record;

    private bool isRecording = false;
    private bool isPlayingBack = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!(movementMode == MovementMode.Record))
            reader = new StreamReader(path);
    }

    // Update is called once per frame
    void Update()
    {
        if (movementMode == MovementMode.Record)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                isRecording = !isRecording;

                if (isRecording && File.Exists(path))
                    File.Delete(path);
            }

            if (isRecording)
            {
                StreamWriter writer = new StreamWriter(path, true);
                writer.WriteLine(this.transform.localPosition.x);
                writer.WriteLine(this.transform.localPosition.z);
                writer.WriteLine(this.GetComponent<CharacterDirectionControl>().speed);
                writer.WriteLine(Time.frameCount);
                writer.WriteLine(Time.deltaTime);
                writer.Close();
            }

        }
        else
        {
            if (movementMode == MovementMode.AutoPlayback)
                isPlayingBack = true;
            else
            {
                if (Input.GetKeyDown(KeyCode.P))
                    isPlayingBack = !isPlayingBack;
            }

            if (isPlayingBack)
            {
                if (!reader.EndOfStream)
                {
                    float nextX = float.Parse(reader.ReadLine());
                    float nextZ = float.Parse(reader.ReadLine());
                    this.GetComponent<CharacterDirectionControl>().speed = float.Parse(reader.ReadLine());
                    float nextFrameCount = float.Parse(reader.ReadLine());
                    float nextDeltaTime = float.Parse(reader.ReadLine());
                    // Set target for Character Direction Control
                    Transform nextPosition = this.GetComponent<CharacterDirectionControl>().transform;
                    nextPosition.localPosition = new Vector3(nextX, nextPosition.localPosition.y, nextZ);
                }
                
            }
        }

    }

    public Vector3 GetStartingPosition()
    {
        StreamReader posReader = new StreamReader(path);
        float startingX = float.Parse(posReader.ReadLine());
        float startingY = float.Parse(posReader.ReadLine());
        posReader.Close();
        return new Vector3(startingX, 0, startingY);
    }

    private void OnApplicationQuit()
    {
        if (!(movementMode == MovementMode.Record))
            reader.Close();
    }
}
