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
            if (!processor.useAllFilesInDirectory)
            {
                processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            }
            processor.processedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.summarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        string[] splitRawDataPath = processor.rawDataDirectory.Split('/');
        GUILayout.Label(splitRawDataPath[splitRawDataPath.Length - 2] + '/' + splitRawDataPath[splitRawDataPath.Length - 1]);

        EditorGUI.BeginChangeCheck();
        processor.useAllFilesInDirectory = GUILayout.Toggle(processor.useAllFilesInDirectory, " Use all");
        if (EditorGUI.EndChangeCheck())
        {
            if (!processor.useAllFilesInDirectory)
            {
                // The toggle was previously on, but is now switched off. In this case we need to choose a specific file.
                processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            }
            processor.processedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.summarizedDataFileName = processor.summarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(processor.useAllFilesInDirectory);

        GUILayout.BeginHorizontal();
        // If not all files are chosen, a specific file need to be indicated.
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth))) 
        {
            processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            processor.processedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.summarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        GUILayout.BeginHorizontal();

        GUILayout.Label(Path.GetFileName(processor.processedDataFileName));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label(Path.GetFileName(processor.summarizedDataFileName));

        GUILayout.EndHorizontal();
    }
}
