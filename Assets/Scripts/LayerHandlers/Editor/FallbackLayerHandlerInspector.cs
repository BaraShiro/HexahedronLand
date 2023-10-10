using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(FallbackLayerHandler))]
public class FallbackLayerHandlerInspector : BlockLayerHandlerInspector
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("The fallback layer handler will always handle the layer regardless of provided data. It will always place the block and will never call another handler.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(blockTypeProp, new GUIContent("Block Type", 
            "The type of block to place in this layer."));
        
        serializedObject.ApplyModifiedProperties();
    }
}