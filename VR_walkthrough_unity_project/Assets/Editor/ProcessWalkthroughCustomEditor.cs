using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(ProcessWalkthrough))]
public class ProcessWalkthroughCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        int buttonWidth = 210;

        ProcessWalkthrough processor = (ProcessWalkthrough) target;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose raw data directory", GUILayout.Width(buttonWidth)))
        {
            processor.rawDataDirectory = EditorUtility.OpenFolderPanel("Choose directory containing raw data", "RawData", "Default");
        }

        string[] splitRawDataPath = processor.rawDataDirectory.Split('/');
        GUILayout.Label(splitRawDataPath[splitRawDataPath.Length - 2] + '/' + splitRawDataPath[splitRawDataPath.Length - 1]);

        processor.useAllFilesInDirectory = GUILayout.Toggle(processor.useAllFilesInDirectory, " Use all");

        GUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(processor.useAllFilesInDirectory);

        GUILayout.BeginHorizontal();
        // If not all files are chosen, a specific file need to be indicated.
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth))) 
        {
            processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
        }

        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Choose processed data file name", GUILayout.Width(buttonWidth)))
        {
            processor.processedDataFileName = EditorUtility.SaveFilePanel("Choose processed data file name", "ProcessedData", "default", "csv");
        }
    }
}
