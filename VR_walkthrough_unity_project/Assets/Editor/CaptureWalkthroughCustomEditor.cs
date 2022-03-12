using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CaptureWalkthrough captureWalkthrough = (CaptureWalkthrough) target;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose file")) 
        {
            string fileName = EditorUtility.SaveFilePanel("Choose file", "RawData", "default", "csv");
            captureWalkthrough.fileName = fileName;
        }
        GUILayout.Label(captureWalkthrough.fileName);

        GUILayout.EndHorizontal();

        captureWalkthrough.sampleInterval = EditorGUILayout.Slider("Sample Interval", captureWalkthrough.sampleInterval, 0.1f, 1.0f);
    }
}
