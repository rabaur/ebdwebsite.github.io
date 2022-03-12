using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CaptureWalkthrough captureWalkthrough = (CaptureWalkthrough) target;

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose subdirectory"))
        {
            captureWalkthrough.directory = EditorUtility.OpenFolderPanel("Choose subdirectory", "RawData", "Default");
        }

        string[] splitDirectories = captureWalkthrough.directory.Split('/');
        GUILayout.Label(splitDirectories[splitDirectories.Length - 2] + '/' + splitDirectories[splitDirectories.Length -1]);

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            // Retrigger file name choice if directory was changed.
            captureWalkthrough.fileName = captureWalkthrough.directory + "/" + Path.GetFileName(captureWalkthrough.fileName);
        }

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose (base) file name")) 
        {
            string fileName = EditorUtility.SaveFilePanel("Select file name", captureWalkthrough.directory, "capture", "csv");
            captureWalkthrough.fileName = fileName;
        }
        GUILayout.Label(Path.GetFileName(captureWalkthrough.fileName));

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        captureWalkthrough.sampleInterval = EditorGUILayout.Slider("Sample Interval", captureWalkthrough.sampleInterval, 0.1f, 1.0f);
    }
}
