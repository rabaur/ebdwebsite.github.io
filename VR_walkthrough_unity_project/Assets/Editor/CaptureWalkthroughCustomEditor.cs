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
        EditorUtility.OpenFilePanel("Choose a folder to save data to");
    }
}
