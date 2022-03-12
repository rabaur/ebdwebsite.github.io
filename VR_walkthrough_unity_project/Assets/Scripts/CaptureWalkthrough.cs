﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CaptureWalkthrough : MonoBehaviour
{
    public float sampleInterval = 0.1f;             // How many seconds have to pass until new sample is taken.
    public string fileName;                         // Name of file the samples get written to.
    public GameObject view;                         // The actual view.
    private List<Vector3> positions;                // List of all positions.
    private List<Vector3> directions;               // List of all directions.
    private List<Vector3> ups;                      // Up axis at each sample.
    private List<Vector3> rights;                   // Right axis at each sample.
    private List<float> yAngle;                     // Azimuth.
    private List<float> xAngle;                     // Elevation.
    private List<float> time;                       // Time.
    private float lastSample;                       // The time the last sample was taken.       

    // Start is called before the first frame update
    void Start()
    {
        // Checking that camera is present.
        bool hasCamera = false;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).GetComponent<Camera>() != null)
            {
                view = gameObject.transform.GetChild(i).gameObject;
                hasCamera = true;
            }
        }
        if (!hasCamera)
        {
            throw new System.Exception("The Player Object this component is attached to has no Camera object child. Please use a valid Player.");
        }

        // Initialize the containers of our data.
        positions = new List<Vector3>();
        directions = new List<Vector3>();
        ups = new List<Vector3>();
        rights = new List<Vector3>();
        yAngle = new List<float>();
        xAngle = new List<float>();
        time = new List<float>();

        Debug.Log("Writing raw data to " + fileName);

        // Set the time of the last sample to the moment the game starts.
        lastSample = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if we have exceeded the desired interval.
        float currTime = Time.realtimeSinceStartup;
        if (currTime - lastSample > sampleInterval)
        {
            // We have exceed the desired interval and set the current time to be the last time we sampled.
            lastSample = currTime;

            // Sample the current position and direction.
            positions.Add(view.transform.position);
            directions.Add(view.transform.forward);
            ups.Add(view.transform.up);
            rights.Add(view.transform.right);
            yAngle.Add(view.transform.rotation.eulerAngles.y);
            xAngle.Add(view.transform.rotation.eulerAngles.x);
            time.Add(currTime);
        }
    }

    // Here the actual IO happens.
    void OnDestroy()
    {

        // Normal file.
        using (StreamWriter openFile = new StreamWriter(fileName)) 
        {
            for (int i = 0; i < positions.Count; i++) {
                openFile.WriteLine(positions[i].ToString());
                openFile.WriteLine(directions[i].ToString());
                openFile.WriteLine(ups[i].ToString());
                openFile.WriteLine(rights[i].ToString());
            }
        }      
    }
}
