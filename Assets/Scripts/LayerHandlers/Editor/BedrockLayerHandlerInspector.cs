using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(BedrockLayerHandler))]
public class BedrockLayerHandlerInspector : BlockLayerHandlerInspector
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("Bedrock is the solid rock foundation at the bottom of the terrain.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        BlockLayerHandlerPropertyFields();
        
        serializedObject.ApplyModifiedProperties();
    }
}

