using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeRayCast : MonoBehaviour
{
    public float radius = 1.0f;
    public float angle = 10.0f;
    public int layers = 10;
    public float len = 10.0f;

    private float radiusPerLayer;
    private int raysPerRadius;
    private List<Vector3> hits;
    private Color[] colors = {Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.grey, Color.white};
    private HashSet<int> IDs = new HashSet<int>();

    // Start is called before the first frame update
    void Start()
    {
        radiusPerLayer = radius / (float) layers;
        raysPerRadius = (int) Mathf.Floor(360.0f / angle);
        Debug.DrawLine
        (Vector3.zero, 100.0f * new Vector3(0.0f, 1.0f, 0.0f), Color.blue, 10.0f);

    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < layers; i++)
        {
            if (i == 0)
            {
                Debug.DrawRay(transform.position, len * transform.forward, Color.red);
                continue;
            }
            for (int j = 0; j < raysPerRadius; j++)
            {
                Vector3 upDev = transform.up * Mathf.Sin(j * angle * Mathf.Deg2Rad) * i * radiusPerLayer;
                Vector3 rightDev = transform.right * Mathf.Cos(j * angle * Mathf.Deg2Rad) * i * radiusPerLayer;
                Vector3 pointOnRadius = transform.position + transform.forward + rightDev + upDev;
                // Debug.DrawRay(transform.position, len * (pointOnRadius - transform.position), Color.red, 1.0f);
                Debug.DrawRay(Vector3.zero, 100.0f * new Vector3(0.0f, 1.0f, 0.0f), Color.blue, 10.0f);
                
                // Physics part.
                // Just struck gold.
                RaycastHit hitInfo;
                if (!Physics.Raycast(transform.position, pointOnRadius - transform.position, out hitInfo))
                    continue;
                MeshCollider meshCollider = hitInfo.collider as MeshCollider;
                if (meshCollider == null || meshCollider.sharedMesh == null)
                    continue;
                GameObject parent = meshCollider.gameObject;
                IDs.Add(meshCollider.gameObject.GetInstanceID());
                if (meshCollider == null || meshCollider.sharedMesh == null)
                {
                    continue;
                }
                Mesh mesh = meshCollider.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Vector3[] vtxAtTri = {  vertices[triangles[hitInfo.triangleIndex * 3 + 0]],
                                        vertices[triangles[hitInfo.triangleIndex * 3 + 1]],
                                        vertices[triangles[hitInfo.triangleIndex * 3 + 2]]};
                Transform hitTransform = hitInfo.collider.transform;
                for (int k = 0; k < 3; k++)
                {
                    vtxAtTri[k] = hitTransform.TransformPoint(vtxAtTri[k]);
                }
                for (int k = 0; k <=3; k++)
                {
                    Debug.DrawLine(vtxAtTri[k % 3], vtxAtTri[(k + 1) % 3], colors[Mathf.Abs(parent.GetInstanceID()) % colors.Length]);
                }
            }
        }
    }

    void OnDestroy()
    {
        foreach (int id in IDs)
        {
            Debug.Log(id);
        }
    }
}
