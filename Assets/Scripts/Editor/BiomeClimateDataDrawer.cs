using System;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomPropertyDrawer(typeof(BiomeClimateData))]
public class BiomeClimateDataDrawer : PropertyDrawer
{
    private readonly GUIStyle rowStyle = new GUIStyle("box");
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        BiomeClimateData biomeClimateData = property.managedReferenceValue as BiomeClimateData;

        EditorGUILayout.Space();

        GUIContent removeButton = EditorGUIUtility.IconContent("Warning@2x");
        removeButton.text = "Remove biome climate data";
        if (GUILayout.Button(removeButton, GUILayout.ExpandWidth(false)))
        {
            property.managedReferenceValue = null;
        }
        
        if (biomeClimateData == null)
        {
            EditorGUILayout.HelpBox("No biome climate data found!", 
                MessageType.Error, true);
            if (GUILayout.Button("Create new biome climate data"))
            {
                property.managedReferenceValue = new BiomeClimateData(1, 1);
            }
        }
        else
        {
            EditorGUI.indentLevel++;
            int newRows = EditorGUILayout.IntField(
                new GUIContent("Precipitation zones", "Number of rows in the climate matrix."),
                biomeClimateData.MatrixRows);
            int newColumns = EditorGUILayout.IntField(
                new GUIContent("Temperature zones", "Number of columns in the climate matrix."),
                biomeClimateData.MatrixColumns);
            EditorGUI.indentLevel--;

            if (GUI.changed)
            {
                biomeClimateData.MatrixRows = newRows;
                biomeClimateData.MatrixColumns = newColumns;
            }

            int rows = biomeClimateData.MatrixRows;
            int columns = biomeClimateData.MatrixColumns;

            EditorGUILayout.Space();
            
            for (int i = 0; i < rows; i++)
            {
                EditorGUILayout.BeginVertical(rowStyle);
                EditorGUILayout.LabelField($"Precipitation {(float)i / rows:0.00} - {(float)(i + 1) / rows:0.00}");
                EditorGUI.indentLevel++;
                for (int j = 0; j < biomeClimateData.MatrixColumns; j++)
                {
                    biomeClimateData.GetBiome(i, j) = EditorGUILayout.ObjectField(
                        $"Temperature {(float)j / columns:0.00} - {(float)(j + 1) / columns:0.00}",
                        biomeClimateData.GetBiome(i, j),
                        typeof(SurfaceBiomeGenerator),
                        true) as SurfaceBiomeGenerator;
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUI.EndProperty();
    }
}