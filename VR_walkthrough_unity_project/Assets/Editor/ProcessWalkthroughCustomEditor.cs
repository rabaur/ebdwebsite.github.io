using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                            //
        // File IO                                                                                                    //
        //                                                                                                            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        GUILayout.Label("File Input and Output", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose raw data directory", GUILayout.Width(buttonWidth)))
        {
            string newRawDirectoryName = EditorUtility.OpenFolderPanel("Choose directory containing raw data", "RawData", "Default");
            
            if (newRawDirectoryName != "")
            {
                if (!processor.useAllFilesInDirectory)
                {
                    processor.rawDataFileName = newRawDirectoryName;
                }
                processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
                processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
            }
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
                string newRawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
                if (newRawDataFileName == "")
                {
                    // The user has aborted the file selection process, but we need to make sure that a valid file is chosen.
                    // We will choose the first file in the directory by default for now.
                    newRawDataFileName = Directory.GetFiles(processor.rawDataDirectory)[0];
                }
                processor.rawDataFileName = newRawDataFileName;
            }
            processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        // If not all files are chosen, a specific file need to be indicated.
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth))) 
        {
            string newRawDatafileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
            if (newRawDatafileName == "")
            {
                // The user has aborted the file-selection process. Revert to old file name.
                newRawDatafileName = processor.rawDataFileName;
            }
            processor.rawDataFileName = newRawDatafileName;
            processor.outProcessedDataFileName = "ProcessedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "processed");
            processor.outSummarizedDataFileName = "SummarizedData/" + processor.CreateDerivedDataFileName(processor.rawDataDirectory, processor.rawDataFileName, "summarized");
        }

        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Processed data file name: " + Path.GetFileName(processor.outProcessedDataFileName));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Summarized data file name: " + Path.GetFileName(processor.outSummarizedDataFileName));

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Delete file", GUILayout.Width(buttonWidth)))
        {
            string fileNameToDelete = EditorUtility.OpenFilePanel("Delete file", "RawData", "csv");
            
            if (fileNameToDelete != "")
            {
                FileUtil.DeleteFileOrDirectory(fileNameToDelete);
            }
        }

        EditorGUILayout.Space();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                            //
        // Visualization                                                                                              //
        //                                                                                                            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        GUILayout.Label("Visualizations", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        processor.visualizeHeatmap = GUILayout.Toggle(processor.visualizeHeatmap, " Heatmap");
        visualizeHeatmapAnimBool.target = processor.visualizeHeatmap;
        if (EditorGUILayout.BeginFadeGroup(visualizeHeatmapAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;
            processor.reuseHeatmap = EditorGUILayout.ToggleLeft("Use processed data file", processor.reuseHeatmap);
            EditorGUI.BeginDisabledGroup(processor.reuseHeatmap);
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
                LayerMask newMask = EditorGUILayout.MaskField("Heatmap Layers", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(processor.layerMask), InternalEditorUtility.layers);
                processor.layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newMask);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel -= 2;
        }
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!processor.generateData);
        processor.visualizeTrajectory = GUILayout.Toggle(processor.visualizeTrajectory, new GUIContent("Trajectory"));
        visualizeTrajectoryAnimBool.target = processor.visualizeTrajectory;
        if (EditorGUILayout.BeginFadeGroup(visualizeTrajectoryAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;

            // Gradient of the trajectory.
            EditorGUI.BeginChangeCheck();
            SerializedObject serializedGradient = new SerializedObject(target);
            SerializedProperty colorGradient = serializedGradient.FindProperty("trajectoryGradient");
            EditorGUILayout.PropertyField(colorGradient, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedGradient.ApplyModifiedProperties();
            }

            processor.chosenTrajectoryVisualizationMethod = EditorGUILayout.Popup(
                "Visualization Method",
                processor.chosenTrajectoryVisualizationMethod, 
                processor.trajectoryVisualizationMethods
            );

            // Width of the trajectory.
            processor.pathWidth = EditorGUILayout.Slider("Trajectory Width", processor.pathWidth, 0.01f, 1.0f);

            // Should shortest path be visualized?
            processor.visualizeShortestPath = EditorGUILayout.ToggleLeft("Visualize Shortest Path", processor.visualizeShortestPath);
            visualizeShortestPathBool.target = processor.visualizeShortestPath;
            if (EditorGUILayout.BeginFadeGroup(visualizeShortestPathBool.faded))
            {
                EditorGUI.indentLevel += 2;

                // Should the start location of the shortest path inferred automatically ot chosen manually.
                processor.inferStartLocation = EditorGUILayout.ToggleLeft(
                    new GUIContent("Infer start location", "Check this if you want the script to automatically infer where the agent has started."), 
                    processor.inferStartLocation
                );
                EditorGUI.BeginDisabledGroup(processor.inferStartLocation);
                {
                    processor.startLocation = EditorGUILayout.ObjectField(
                        new GUIContent("Start Location", "The gameobject that corresponds to the start location"), 
                        processor.startLocation, typeof(Transform), true
                    ) as Transform;
                }
                EditorGUI.EndDisabledGroup();

                // Setting the endlocation.
                processor.endLocation = EditorGUILayout.ObjectField(
                    new GUIContent("End Location", "The gameobject that corresponds to the end location"), 
                    processor.endLocation, typeof(Transform), true
                ) as Transform;

                EditorGUI.BeginChangeCheck();
                SerializedObject serializedGradient1 = new SerializedObject(target);
                SerializedProperty shortestPathGradient = serializedGradient1.FindProperty("shortestPathGradient");
                EditorGUILayout.PropertyField(shortestPathGradient, true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedGradient1.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.indentLevel -= 2;
            EditorGUILayout.EndFadeGroup();
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUI.EndDisabledGroup();
    }

    private void HorizontalSeperator()
    {
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
    }
}
