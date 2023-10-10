using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(ParentrockLayerHandler))]
public class ParentrockLayerHandlerInspector : BlockLayerHandlerInspector
{
    private SerializedProperty layerThicknessProp;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        layerThicknessProp = serializedObject.FindProperty("layerThickness");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("Parentrock is the rock layer beneath the soil from which soil is formed.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        BlockLayerHandlerPropertyFields();
        
        EditorGUILayout.Space();

        EditorGUILayout.IntSlider(layerThicknessProp, 0, 10, new GUIContent("Layer Thickness",
            "The number of blocks to pad the terrain height with."));

        serializedObject.ApplyModifiedProperties();
    }
}

