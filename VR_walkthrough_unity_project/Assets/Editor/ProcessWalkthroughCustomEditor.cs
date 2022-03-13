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

        processor.generateData = GUILayout.Toggle(processor.generateData, "Generate data from raw data file");

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!processor.generateData);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose raw data directory", GUILayout.Width(buttonWidth)))
        {
            processor.rawDataDirectory = EditorUtility.OpenFolderPanel("Choose directory containing raw data", "RawData", "Default");
            if (!processor.useAllFilesInDirectory)
            {
                processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            }
            processor.outProcessedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outPummarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
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
            processor.outProcessedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outPummarizedDataFileName = processor.outPummarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(processor.useAllFilesInDirectory);

        GUILayout.BeginHorizontal();
        // If not all files are chosen, a specific file need to be indicated.
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth))) 
        {
            processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            processor.outProcessedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outPummarizedDataFileName = processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Processed data file name: " + Path.GetFileName(processor.outProcessedDataFileName));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Summarized data file name: " + Path.GetFileName(processor.outPummarizedDataFileName));

        GUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(processor.generateData);
        if (GUILayout.Button("Choose processed data file")) {
            processor.inProcessedDataFileName = EditorUtility.OpenFilePanel("Choose processed data file", "ProcessedData", "csv");

            // Find corresponding statistic file.
            string fileNameOnly = Path.GetFileNameWithoutExtension(processor.inProcessedDataFileName);
            string[] splitFileNameOnly = fileNameOnly.Split('_');

            string inStatisticFileName = "";
            for (int i = 0; i < processor.inProcessedDataFileName.Split('/').Length - 1; i++)
            {
                inStatisticFileName += processor.inProcessedDataFileName.Split('/')[i] + "/";
            }
            inStatisticFileName += splitFileNameOnly[0] + "_summarized.csv";
            processor.inStatisticFileName = inStatisticFileName;
        }

        GUILayout.Label("Reusing processed data file: " + Path.GetFileName(processor.inProcessedDataFileName));
        GUILayout.Label("Reusing statistic data file: " + Path.GetFileName(processor.inStatisticFileName));
        EditorGUI.EndDisabledGroup();
    }
}
