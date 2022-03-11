using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Press me!"))
        {
            Debug.Log("UWU. I was pressed.");
        }
        if (GUILayout.Button("Load file to process."))
        {
            Debug.Log(EditorUtility.OpenFilePanel("Choose file to load data to visualize.", "CapturedData", "csv"));
        }
    }
}
