using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(AirLayerHandler))]
public class AirLayerHandlerInspector : BlockLayerHandlerInspector
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("Air is the empty space not occupied by blocks of soil, stone, etc.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        BlockLayerHandlerPropertyFields();
        
        serializedObject.ApplyModifiedProperties();
    }
}

