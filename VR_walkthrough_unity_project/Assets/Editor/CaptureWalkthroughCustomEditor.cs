using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CaptureWalkthrough capture = (CaptureWalkthrough) target;

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose subdirectory"))
        {
            capture.directory = EditorUtility.OpenFolderPanel("Choose subdirectory", "RawData", "Default");
        }

        string[] splitDirectories = capture.directory.Split('/');
        GUILayout.Label(splitDirectories[splitDirectories.Length - 2] + '/' + splitDirectories[splitDirectories.Length -1]);

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            // Retrigger file name choice if directory was changed.
            capture.fileName = capture.directory + "/" + Path.GetFileName(capture.fileName);
        }

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose (base) file name")) 
        {
            string fileName = EditorUtility.SaveFilePanel("Select file name", capture.directory, "capture", "csv");
            capture.fileName = fileName;
        }
        GUILayout.Label(Path.GetFileName(capture.fileName));

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        capture.sampleInterval = EditorGUILayout.Slider("Sample Interval", capture.sampleInterval, 0.1f, 1.0f);
    }
}
