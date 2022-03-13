using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ProcessWalkthrough : MonoBehaviour
{
    // Public variables.
    public LayerMask layerMask;
    public Gradient heatmapGradient;
    public bool generateData = true;

    // IO-related public variables.
    public string inPathWalkthrough;
    public string inPathHeatmapData;
    public string outDirHeatmap;
    public string outFileNameHeatmap;
    public string outDirStatistic;
    public string outFileNameStatistic;

    // Public variables concerned with the raycast.
    public float horizontalViewAngle = 90.0f;
    public float verticalViewAngle = 60.0f;
    public int raysPerRaycast = 100;

    // Private variables concerned with the raycast.
    private float outerConeRadiusHorizontal;
    private float outerConeRadiusVertical;
    private float interSubconeDisplacement;
    private float intraSubconeDisplacementAngle;

    // Visualization-related public variables.
    public float particleSize = 1.0f;
    public float h = 1.0f;

    public bool useAllFilesInDirectory = false;
    public string rawDataDirectory = "RawData/Default";
    public string rawDataFileName = "RawData/Default/default.csv";
    public string outProcessedDataFileName;
    public string outSummarizedDataFileName;
    public string inProcessedDataFileName;
    public string inSummarizedDataFileName;
    private List<Vector3> hits = new List<Vector3>();
    private float[] colors;
    private int[] hitsPerLayer;
    public bool visualizeHeatmap = false;
    public bool visualizeTrajectory = false;
    private Vector3[] particlePositions;
    public Gradient trajectoryGradient;
    public bool visualizeShortestPath = false;
    public bool inferStartLocation = true;
    public Transform startLocation;
    public Transform endLocation;
    private Vector3[] trajectoryPositions;
    private Vector3[] trajectoryDirections;
    private Vector3[] trajectoryUpVectors;
    private Vector3[] trajectoryRightVectors;
    public bool reuseHeatmap = true;
    public float pathWidth = 0.1f;
    private GameObject lineRendererParent;
    private LineRenderer lineRenderer;

    void Start()
    {
        outerConeRadiusHorizontal = Mathf.Tan((horizontalViewAngle / 2.0f) * Mathf.Deg2Rad);
        outerConeRadiusVertical = Mathf.Tan((verticalViewAngle / 2.0f) * Mathf.Deg2Rad);
        ReadRawFile();
        if (visualizeHeatmap)
        {
            if (reuseHeatmap)
            {
                LoadHeatMap();
            }
            else
            {
                CreateHeatMap();
                SaveProcessedDataFile();
                SaveStatisticDataFile();
            }
            CreateParticles();
        }
        if (visualizeTrajectory)
        {
            lineRendererParent = new GameObject();
            lineRenderer = lineRendererParent.AddComponent<LineRenderer>();
            VisualizeTrajectory(lineRenderer, new List<Vector3>(trajectoryPositions), trajectoryGradient, pathWidth);
        }
    }

    /* Converts string-representation of vector (in format of Vector3.ToString()) to Vector3.
     * @param str       string representation of vector.
     * @out             Vector3 representation of input string.
     */
    Vector3 str2Vec(string str)
    {
        str = str.Substring(1, str.Length - 2);
        string[] substrs = str.Split(',');
        return new Vector3( float.Parse(substrs[0]), 
                            float.Parse(substrs[1]), 
                            float.Parse(substrs[2]));
    }

    /* Returns a set points corresponding to collisions of rays from a cone-shaped raycast.
     * @param viewPoint         Vector corresponding to the view-point.
     * @param forward           Vector corresponging to the direction of sight.
     * @param vertical          Vector corresponding to the vertical axis of the cone.
     * @param horizontal        Vector corresponding to the horizontal axis of the cone.
     * @return                  List of vectors corresponding to the collisions of the cone-raycast with the environment.
     */
    List<Vector3> castAndCollide(Vector3 viewPoint, Vector3 forward, Vector3 vertical, Vector3 horizontal, ref int[] hitsPerLayer)
    {
        Vector3 hitPos = Vector3.zero;
        List<Vector3> results = new List<Vector3>();
        for(int i = 0; i < raysPerRaycast; i++)
        {
            Vector3 p = viewPoint
                        + forward
                        + vertical * Random.value * outerConeRadiusVertical * Mathf.Sin(2.0f * Mathf.PI * Random.value)
                        + horizontal * Random.value * outerConeRadiusHorizontal * Mathf.Sin(2.0f * Mathf.PI * Random.value);
            if (collision(viewPoint, p - viewPoint, ref hitPos, ref hitsPerLayer))
            {
                results.Add(hitPos);
            }
        }

        return results;
    }

    /* Casts a ray and returns the position of collision if ray hits object in specified layers.
     * @param start             Vector corresponding to the start-position of the ray.
     * @param dir               Vector corresponding to the direction of the ray.
     * @param hitPos            Will be overwritten with position of hit if it occurs.
     * @return                  If hits object in specified layer: true. Else: false.
     */
    bool collision(Vector3 start, Vector3 dir, ref Vector3 hitPos, ref int[] hitsPerLayer)
    {
        // If a hit occurs, this will hold all the information about it.
        RaycastHit hit;

        // Casting the ray and checking for collision.
        if (!Physics.Raycast(start, dir, out hit))
        {
            return false;
        }

        
        // The MeshCollider the ray hit. NULL-check.
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            return false;
        }

        // Check if it is in the desired layer.
        if (!(layerMask == (layerMask | (1 << meshCollider.gameObject.layer))))
        {
            return false;
        }
        hitsPerLayer[meshCollider.gameObject.layer] += 1;
        hitPos = hit.point;
        return true;
    }

    /* Makes sure filename is unique and output directory exists.
     * @param dirName       Name of directory.
     * @param fileName      Proposed name of file.
     * @param format        Format of file.
     * @out                 Unique file-name.
     */
    string makeFileNameUnique(string dirName, string fileName, string format)
    {
        // Create directory if does not exist.
        Directory.CreateDirectory(dirName);

        // This is the path the file will be written to.
        string path = dirName + Path.DirectorySeparatorChar + fileName + "." + format;
        
        // Check if specified file exists yet and if user wants to overwrite.
        if (File.Exists(path))
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
        }
        return path;
    }

    void CreateParticles() 
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particlePositions.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].position = particlePositions[i];
            particles[i].velocity = Vector3.zero;
            particles[i].size = particleSize;
            particles[i].color = heatmapGradient.Evaluate(colors[i]);
        }
        ParticleSystem partSys = GetComponent<ParticleSystem>();
        partSys.SetParticles(particles, particles.Length);
    }

    void Update()
    {
        if (visualizeTrajectory)
        {
            VisualizeTrajectory(lineRenderer, new List<Vector3>(trajectoryPositions), trajectoryGradient, pathWidth);
        }
    }

    public string CreateDerivedDataFileName(string rawDataDirectory, string rawDataFileName, string type)
    {
        if (useAllFilesInDirectory)
        {
            string[] splitRawDataDirectory = rawDataDirectory.Split('/');
            return "all_files_in_" + splitRawDataDirectory[splitRawDataDirectory.Length - 1] + "_" + type + ".csv";
        }
        return Path.GetFileNameWithoutExtension(rawDataFileName) + "_" + type + Path.GetExtension(rawDataFileName);
    }

    private void ReadRawFile()
    {
        // Reading in the data from a walkthough.
        string[] data = File.ReadAllLines(rawDataFileName);
        trajectoryPositions = new Vector3[data.Length / 4];
        trajectoryDirections = new Vector3[data.Length / 4];
        trajectoryUpVectors = new Vector3[data.Length / 4];
        trajectoryRightVectors = new Vector3[data.Length / 4];
        for (int i = 0; i < data.Length / 4; i++)
        {
            trajectoryPositions[i] = str2Vec(data[4 * i + 0]);
            trajectoryDirections[i] = str2Vec(data[4 * i + 1]);
            trajectoryUpVectors[i] = str2Vec(data[4 * i + 2]);
            trajectoryRightVectors[i] = str2Vec(data[4 * i + 3]);
        }
    }

    private void CreateHeatMap()
    {
        // Reading in the data from a walkthough.
        string[] data = File.ReadAllLines(rawDataFileName);
        Vector3[] trajectoryPositions = new Vector3[data.Length / 4];
        Vector3[] directions = new Vector3[data.Length / 4];
        Vector3[] ups = new Vector3[data.Length / 4];
        Vector3[] rights = new Vector3[data.Length / 4];
        for (int i = 0; i < data.Length / 4; i++)
        {
            trajectoryPositions[i] = str2Vec(data[4 * i + 0]);
            directions[i] = str2Vec(data[4 * i + 1]);
            ups[i] = str2Vec(data[4 * i + 2]);
            rights[i] = str2Vec(data[4 * i + 3]);
        }

        // Will hold all the positions where the rays hit.
        hits = new List<Vector3>();

        // Unity generates 32 layers per default.
        hitsPerLayer = new int[32];
        for (int i = 0; i < data.Length / 4; i++)
        {
            hits.AddRange(castAndCollide(trajectoryPositions[i], directions[i], ups[i], rights[i], ref hitsPerLayer));
        }

        int n = hits.Count;
        
        // Calculate the distances between each hit.
        List<float> distances = new List<float>();
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                distances.Add(Vector3.Distance(hits[j], hits[i]));
            }
        }

        float[] avgDistances = new float[hits.Count];
        for (int i = 0; i < n; i++)
        {
            float avg = 0;
            int offset = 0;
            int C = 0;
            for (int j = 1; j <= i; j++)
            {
                avg += Mathf.Exp(-distances[offset + i - j] / h);
                offset += n - j;
                C++;
            }
            for (int j = 0; j < n - i - 1; j++)
            {
                avg += Mathf.Exp(-distances[offset + j] / h);
                C++;
            }
            avgDistances[i] = avg / (C * h);
        }
        float min = float.MaxValue;
        float max = 0.0f;
        for (int i = 0; i < n; i++)
        {
            min = avgDistances[i] < min ? avgDistances[i] : min;
            max = avgDistances[i] > max ? avgDistances[i] : max;
        }

        float range = max - min;

        colors = new float[hits.Count];
        for (int i = 0; i < hits.Count; i++)
        {
            colors[i] = (avgDistances[i] - min) / range;
        }
        particlePositions = hits.ToArray();
    }

    private void LoadHeatMap()
    {
        // Reading in the heatmap-data from prior processing and creating arrays for positions and colors \in [0, 1].
        string[] allLines = File.ReadAllLines(inProcessedDataFileName);
        particlePositions = new Vector3[allLines.Length];
        colors = new float[allLines.Length];
        for (int i = 0; i < allLines.Length; i++)
        {
            string[] line = allLines[i].Split(',');
            if (line.Length == 4)
            {
                particlePositions[i] = str2Vec(line[0] + "," + line[1] + "," + line[2]);
                colors[i] = float.Parse(line[3]);
            }
        }
    }

    private void SaveProcessedDataFile()
    {
        using (StreamWriter processedDataFile = new StreamWriter(outProcessedDataFileName))
        {
            for (int i = 0; i < hits.Count; i++)
            {
                processedDataFile.WriteLine(hits[i]+","+colors[i]);
            }
        }
    }

    private void SaveStatisticDataFile()
    {
        // Determine the total number of hits.
        int totalHits = 0;
        for (int i = 0; i < hitsPerLayer.Length; i++)
        {
            totalHits += hitsPerLayer[i];
        }
        using (StreamWriter statisticDataFile = new StreamWriter(outSummarizedDataFileName))
        {
            for (int i = 0; i < hitsPerLayer.Length; i++)
            {
                if (hitsPerLayer[i] != 0)
                {
                    statisticDataFile.WriteLine(LayerMask.LayerToName(i)+ "," +(((float) hitsPerLayer[i]) / ((float) totalHits)));
                }
            }
        }
    }

    private void VisualizeTrajectory(LineRenderer lineRenderer, List<Vector3> positions, Gradient gradient, float trajectoryWidth)
    {
        // Setting up the visualization things.
        lineRenderer.colorGradient = gradient;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = trajectoryWidth;
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }
}
