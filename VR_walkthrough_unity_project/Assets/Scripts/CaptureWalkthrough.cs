using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CaptureWalkthrough : MonoBehaviour
{
    public float sampleInterval = 0.1f;             // How many seconds have to pass until new sample is taken.
    public string dirName = "walkthroughs";         // The name of the directory the walkthrough will get saved to.
    public string fileName = "default";             // Name of file the samples get written to.
    public string format = "csv";                   // The file format.
    public bool forceWrite = false;                 // Set to true if you want to overwrite the file.
    public GameObject view;                         // The actual view.

    private StreamWriter file;                      // This is the file we are going to write to.
    private StreamWriter specFile;                  // File for custom format.
    private string path;                            // Path to the file.
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
        // Initialize the containers of our data.
        positions = new List<Vector3>();
        directions = new List<Vector3>();
        ups = new List<Vector3>();
        rights = new List<Vector3>();
        yAngle = new List<float>();
        xAngle = new List<float>();
        time = new List<float>();

        // Create directory if does not exist.
        Directory.CreateDirectory(dirName);

        // This is the path the file will be written to.
        path = dirName + Path.DirectorySeparatorChar + fileName + "." + format;
        string specPath = "";
        
        // Check if specified file exists yet and if user wants to overwrite.
        if (File.Exists(path) && !forceWrite)
        {
            /* In this case we need to make the filename unique.
             * We will achiece that by:
             * foldername + sep + filename + . + format -> foldername + sep + filename + _x + . format
             * x will be increased in case of multiple overwrites.
             */
            
            // Check if there was a previous overwrite and get highest identifier.
            int id = 0;
            while (File.Exists(dirName + Path.DirectorySeparatorChar + fileName + "_" + id.ToString() + "." + format))
            {
                id++;
            }

            // Now we have found a unique identifier and create the new name.
            path = dirName + Path.DirectorySeparatorChar + fileName + "_" + id.ToString() + "." + format;
            specPath = dirName + Path.DirectorySeparatorChar + fileName + "_" + id.ToString() + "_rhino." + format;
        }

        Debug.Log(path);
        Debug.Log(specPath);
        // Open file.
        file = new StreamWriter(path);
        specFile = new StreamWriter(specPath);

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
        for (int i = 0; i < positions.Count; i++)
        {
            file.WriteLine(positions[i].ToString());
            file.WriteLine(directions[i].ToString());
            file.WriteLine(ups[i].ToString());
            file.WriteLine(rights[i].ToString());
        }

        // Special file.
        specFile.WriteLine("Time,X,Y,Z,Viewazimuth,ViewElevation");
        for (int i = 0; i < positions.Count; i++)
        {
            string line = "";
            line += time[i].ToString() + ",";
            line += positions[i].x.ToString() + ",";
            line += positions[i].y.ToString() + ",";
            line += positions[i].z.ToString() + ",";
            line += yAngle[i].ToString() + ",";
            line += xAngle[i].ToString();
            specFile.WriteLine(line);
        }
        file.Close();
        specFile.Close();        
    }
}
