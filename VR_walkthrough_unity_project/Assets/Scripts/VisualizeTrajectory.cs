using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VisualizeTrajectory : MonoBehaviour
{
    public string filePath;                         // Input file-path of walkthrough.
    public Gradient trajectoryColor;                // Color of the trajectory.
    public float trajectoryWidth = 0.2f;            // Width of the trajectory.
    public float distance;                          // Distance covered by the agent in the walkthrough.
    private LineRenderer lineRenderer;              // Renderer used to visualize trajectory.

    // Start is called before the first frame update
    void Start()
    {
        // Setting up the visualization things.
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.colorGradient = trajectoryColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = trajectoryWidth;
        List<Vector3> positions = new List<Vector3>();
        string[] lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i+=4) {
            positions.Add(str2Vec(lines[i]));
        }

        distance = 0.0f;
        for (int i = 1; i < positions.Count; i++) {
            distance += Vector3.Distance(positions[i], positions[i - 1]);
        }
        Debug.Log("Distance of " + filePath + ": " + distance);
        
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     Vector3 str2Vec(string str)
    {
        str = str.Substring(1, str.Length - 2);
        string[] substrs = str.Split(',');
        return new Vector3( float.Parse(substrs[0]), 
                            float.Parse(substrs[1]), 
                            float.Parse(substrs[2]));
    }
}
