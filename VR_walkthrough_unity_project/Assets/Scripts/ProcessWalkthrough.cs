using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.AI;

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
    public Gradient shortestPathGradient;
    public bool visualizeShortestPath = false;
    public bool inferStartLocation = true;
    public Transform startLocation;
    public Transform endLocation;
    private List<Vector3[]> trajectoryPositions;
    private List<Vector3[]> trajectoryForwardDirections;
    private List<Vector3[]> trajectoryUpDirections;
    private List<Vector3[]> trajectoryRightDirections;
    public bool reuseHeatmap = true;
    public float pathWidth = 0.1f;
    private GameObject lineRendererParent;
    private LineRenderer lineRenderer;
    private GameObject shortestPathLinerendererParent;
    private LineRenderer shortestPathLinerenderer;
    private int numFiles;
    public Material lineRendererMaterial;

    void Start()
    {
        outerConeRadiusHorizontal = Mathf.Tan((horizontalViewAngle / 2.0f) * Mathf.Deg2Rad);
        outerConeRadiusVertical = Mathf.Tan((verticalViewAngle / 2.0f) * Mathf.Deg2Rad);
        trajectoryPositions = new List<Vector3[]>();
        trajectoryForwardDirections = new List<Vector3[]>();
        trajectoryUpDirections = new List<Vector3[]>();
        trajectoryRightDirections = new List<Vector3[]>();

        // Create a list of filenames for the raw data files to be read. If <useAllFilesInDirectory> is false, then this
        // list will consist of only one file. Otherwise all files in that directory will be added.
        List<string> rawDataFileNames = new List<string>();
        if (useAllFilesInDirectory)
        {
            // Read in all files in the directory.
            rawDataFileNames = new List<string>(Directory.GetFiles(rawDataDirectory, "*.csv"));
        }
        else 
        {
            // Only get single file.
            rawDataFileNames.Add(rawDataFileName);
        }

        numFiles = rawDataFileNames.Count;
        Debug.Log(numFiles);

        // TODO: Remove after debug.
        foreach (string fileName in rawDataFileNames)
            Debug.Log(fileName);

        // Parse each file and populate the positions and direction arrays.
        foreach (string fileName in rawDataFileNames)
        {
            (Vector3[], Vector3[], Vector3[], Vector3[]) parsedData = ReadRawFile(fileName);
            trajectoryPositions.Add(parsedData.Item1);
            trajectoryForwardDirections.Add(parsedData.Item2);
            trajectoryUpDirections.Add(parsedData.Item3);
            trajectoryRightDirections.Add(parsedData.Item4);
        }        

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
                WriteSummarizedDataFile();
            }
            CreateParticles();
        }
        if (visualizeTrajectory)
        {
            foreach (Vector3[] currPositions in trajectoryPositions)
            {
                lineRendererParent = new GameObject();
                lineRendererParent.hideFlags = HideFlags.HideInHierarchy;
                lineRenderer = lineRendererParent.AddComponent<LineRenderer>();
                VisualizeTrajectory(lineRenderer, new List<Vector3>(currPositions), trajectoryGradient, pathWidth);
                if (visualizeShortestPath)
                {
                    Vector3 startPos = inferStartLocation ? currPositions[0] : startLocation.position;
                    Vector3 endPos = endLocation.position;

                    // startPos and endPos do not necessarily lie on the NavMesh. Finding path between them might fail.
                    NavMeshHit startHit;
                    NavMesh.SamplePosition(startPos, out startHit, 100.0f, NavMesh.AllAreas);  // Hardcoded to 100 units of maximal distance.
                    startPos = startHit.position;
                    NavMeshHit endHit;
                    NavMesh.SamplePosition(endPos, out endHit, 100.0f, NavMesh.AllAreas);
                    endPos = endHit.position;

                    // Creating linerenderer for shortest path.
                    shortestPathLinerendererParent = new GameObject();
                    shortestPathLinerendererParent.hideFlags = HideFlags.HideInHierarchy;
                    shortestPathLinerenderer = shortestPathLinerendererParent.AddComponent<LineRenderer>();

                    // Create shortest path.
                    NavMeshPath navMeshPath = new NavMeshPath();
                    NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath);
                    VisualizeTrajectory(shortestPathLinerenderer, new List<Vector3>(navMeshPath.corners), shortestPathGradient, pathWidth);
                }
            }
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
    List<Vector3> CastAndCollide(Vector3 viewPoint, Vector3 forward, Vector3 vertical, Vector3 horizontal, ref int[] hitsPerLayer)
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
            /*
            VisualizeTrajectory(lineRenderer, new List<Vector3>(trajectoryPositions), trajectoryGradient, pathWidth);
            */
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

    private (Vector3[], Vector3[], Vector3[], Vector3[]) ReadRawFile(string rawDataFileName)
    {
        // Reading in the data from a walkthough.
        string[] data = File.ReadAllLines(rawDataFileName);
        Vector3[] positions = new Vector3[data.Length / 4];
        Vector3[] forwardDirections = new Vector3[data.Length / 4];
        Vector3[] upDirections = new Vector3[data.Length / 4];
        Vector3[] rightDirections = new Vector3[data.Length / 4];
        for (int i = 0; i < data.Length / 4; i++)
        {
            positions[i] = str2Vec(data[4 * i + 0]);
            forwardDirections[i] = str2Vec(data[4 * i + 1]);
            upDirections[i] = str2Vec(data[4 * i + 2]);
            rightDirections[i] = str2Vec(data[4 * i + 3]);
        }

        return (positions, forwardDirections, upDirections, rightDirections);
    }

    private void CreateHeatMap()
    {

        // Will hold all the positions where the rays hit.
        hits = new List<Vector3>();

        // Unity generates 32 layers per default.
        hitsPerLayer = new int[32];

        for (int i = 0; i < numFiles; i++)
        {
            Vector3[] currPositions = trajectoryPositions[i];
            Vector3[] currForwardDirections = trajectoryForwardDirections[i];
            Vector3[] currUpDirections = trajectoryUpDirections[i];
            Vector3[] currRightDirections = trajectoryRightDirections[i];
            Debug.Log(currPositions.Length);
            for (int j = 0; j < currPositions.Length; j++)
            {
                hits.AddRange(
                    CastAndCollide(
                        currPositions[j],
                        currForwardDirections[j],
                        currUpDirections[j],
                        currRightDirections[j],
                        ref hitsPerLayer
                    )
                );
            }
        }

        int n = hits.Count;
        Debug.Log($"number of hits: {n}");
        
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

    private void WriteSummarizedDataFile()
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
        lineRenderer.colorGradient = gradient;
        lineRenderer.material = lineRendererMaterial;
        lineRenderer.widthMultiplier = trajectoryWidth;
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    private Mesh CreateTrajectoryMesh(Vector3[] positions, Vector3[] upDirections, Vector3[] rightDirections, int resolution, float thickness)
    {
        int numPos = positions.Length;
        float radIncrement = 2 * Mathf.PI / resolution;

        // For each position in the trajectory, we create a surrounding ring of <resolution> vertices.
        Vector3[] vertices = new Vector3[numPos * resolution];
        Vector3[] normals = new Vector3[numPos * resolution];
        Color[] colors = new Color[numPos * resolution];

        // Calculate maximal distance between timesteps.
        float maxDist = 0.0f;
        for (int i = 0; i < numPos - 1; i++)
        {
            float currDist = Vector3.Distance(positions[i + 1], positions[i]);
            maxDist = currDist > maxDist ? currDist : maxDist;
        }

        Color currColor = Color.black; // Initialize as black.

        for (int i = 0; i < numPos; i++)
        {
            Vector3 currPos = positions[i];
            Vector3 currRight = rightDirections[i];
            Vector3 currUp = upDirections[i];

            if (i >= 1 && i < numPos - 1)
            {
                // Averaged direction of segments adjacent to current position.
                Vector3 lastDir = positions[i] - positions[i - 1];
                Vector3 nextDir = positions[i + 1] - positions[i];
                Vector3 ringPlaneNormal = (lastDir + nextDir) / 2;

                // Project right vector and up vector onto this new plane.
                currRight = Vector3.Normalize(Vector3.ProjectOnPlane(currRight, ringPlaneNormal));
                currUp = Vector3.Normalize(Vector3.ProjectOnPlane(currUp, ringPlaneNormal));
            }

            if (i < numPos - 1)
            {
                currColor = trajectoryGradient.Evaluate(Vector3.Distance(positions[i + 1], positions[i]) / maxDist);
            }

            // The surrounding ring lies on the plane spanned by the up and right vectors.
            for (int j = 0; j < resolution; j++)
            {
                Vector3 vertex = positions[i] + thickness * Mathf.Cos(radIncrement * j) * currRight + thickness * Mathf.Sin(radIncrement * j) * currUp;
                normals[i * resolution + j] = Mathf.Cos(radIncrement * j) * currRight + Mathf.Sin(radIncrement * j) * currUp;
                Debug.DrawRay(vertex, 0.1f * normals[i * resolution + j], Color.blue, 120.0f);
                vertices[i * resolution + j] = vertex;
                colors[i * resolution + j] = currColor;
            }
        }

        // The forward triangles:
        int[] triangles = new int[(numPos - 1) * (resolution - 1) * 2 * 3];
        int triIdx = 0;
        for (int i = 0; i < numPos - 1; i++)
        {
            for (int j = 0; j < resolution - 1; j++)
            {
                triangles[triIdx * 3] = i * resolution + j;
                triangles[triIdx * 3 + 1] = i * resolution + j + 1;
                triangles[triIdx * 3 + 2] = (i + 1) * resolution + j;
                triIdx++;
            }
        }

        // The backward triangles:
        for (int i = 1; i < numPos; i++)
        {
            for (int j = 1; j < resolution; j++)
            {
                triangles[triIdx * 3] = i * resolution + j;
                triangles[triIdx * 3 + 1] = i * resolution + j - 1;
                triangles[triIdx * 3 + 2] = (i - 1) * resolution + j;
                triIdx++;
            }
        }

        Mesh mesh = new Mesh();
        GameObject go = new GameObject();
        go.AddComponent<MeshFilter>();
        go.GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.colors = colors;
        go.AddComponent<MeshRenderer>();
        return mesh;
    }
}
