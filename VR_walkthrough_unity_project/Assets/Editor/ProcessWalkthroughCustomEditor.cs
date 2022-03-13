using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(ProcessWalkthrough))]
public class ProcessWalkthroughCustomEditor : Editor
{
    UnityEditor.AnimatedValues.AnimBool visualizeTrajectoryAnimBool;
    UnityEditor.AnimatedValues.AnimBool visualizeHeatmapAnimBool;
    UnityEditor.AnimatedValues.AnimBool visualizeShortestPathBool;
    ProcessWalkthrough processor;
    int buttonWidth = 210;

    private void OnEnable()
    {
        processor = (ProcessWalkthrough) target;
        visualizeTrajectoryAnimBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeTrajectory);
        visualizeHeatmapAnimBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeHeatmap);
        visualizeShortestPathBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeShortestPath);
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        EditorGUILayout.Space();

        GUILayout.Label("File Input and Output", EditorStyles.boldLabel);

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
            processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
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
            processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(processor.useAllFilesInDirectory);

        GUILayout.BeginHorizontal();
        // If not all files are chosen, a specific file need to be indicated.
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth))) 
        {
            Debug.Log("here");
            processor.rawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Processed data file name: " + Path.GetFileName(processor.outProcessedDataFileName));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Summarized data file name: " + Path.GetFileName(processor.outSummarizedDataFileName));

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
            processor.inSummarizedDataFileName = inStatisticFileName;
        }

        GUILayout.Label("Reusing processed data file: " + Path.GetFileName(processor.inProcessedDataFileName));
        GUILayout.Label("Reusing statistic data file: " + Path.GetFileName(processor.inSummarizedDataFileName));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        EditorGUILayout.Space();

        GUILayout.Label("Visualizations", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        processor.visualizeHeatmap = GUILayout.Toggle(processor.visualizeHeatmap, " Heatmap");
        visualizeHeatmapAnimBool.target = processor.visualizeHeatmap;
        if (EditorGUILayout.BeginFadeGroup(visualizeHeatmapAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;
            processor.raysPerRaycast = EditorGUILayout.IntSlider("Rays per Raycast", processor.raysPerRaycast, 1, 200);
            processor.particleSize = EditorGUILayout.Slider("Particle Size", processor.particleSize, 0.1f, 5.0f);
            processor.h = EditorGUILayout.Slider("Blur", processor.h, 0.1f, 10.0f);

            EditorGUI.BeginChangeCheck();
            SerializedObject serializedGradient1 = new SerializedObject(target);
            SerializedProperty colorGradient1 = serializedGradient1.FindProperty("heatmapGradient");
            EditorGUILayout.PropertyField(colorGradient1, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedGradient1.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel -= 2;
        }
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!processor.generateData);
        processor.visualizeTrajectory = GUILayout.Toggle(processor.visualizeTrajectory, new GUIContent("Trajectory", "Enable \"Generate data from raw data file\" to use this option"));
        visualizeTrajectoryAnimBool.target = processor.visualizeTrajectory;
        if (EditorGUILayout.BeginFadeGroup(visualizeTrajectoryAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;
            EditorGUI.BeginChangeCheck();
            SerializedObject serializedGradient = new SerializedObject(target);
            SerializedProperty colorGradient = serializedGradient.FindProperty("trajectoryGradient");
            EditorGUILayout.PropertyField(colorGradient, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedGradient.ApplyModifiedProperties();
            }
            processor.visualizeShortestPath = EditorGUILayout.ToggleLeft("Visualize Shortest Path", processor.visualizeShortestPath);
            visualizeShortestPathBool.target = processor.visualizeShortestPath;
            if (EditorGUILayout.BeginFadeGroup(visualizeShortestPathBool.faded))
            {
                EditorGUI.indentLevel += 2;
                processor.inferStartLocation = EditorGUILayout.ToggleLeft(new GUIContent("Infer start location", "Check this if you want the script to automatically infer where the agent has started."), processor.inferStartLocation);
                EditorGUI.BeginDisabledGroup(processor.inferStartLocation);
                {
                    processor.startLocation = EditorGUILayout.ObjectField(new GUIContent("Start Location", "The gameobject that corresponds to the start location"), processor.startLocation, typeof(Transform), true) as Transform;
                }
                EditorGUI.EndDisabledGroup();
                processor.endLocation = EditorGUILayout.ObjectField(new GUIContent("End Location", "The gameobject that corresponds to the end location"), processor.endLocation, typeof(Transform), true) as Transform;
                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.indentLevel -= 2;
            EditorGUILayout.EndFadeGroup();
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUI.EndDisabledGroup();
    }
}
