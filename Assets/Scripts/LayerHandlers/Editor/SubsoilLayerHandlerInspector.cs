using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(SubsoilLayerHandler))]
public class SubsoilLayerHandlerInspector : BlockLayerHandlerInspector
{
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("Subsoil is the soil layer above the parentrock and below the top soil.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        BlockLayerHandlerPropertyFields();
        
        serializedObject.ApplyModifiedProperties();
    }
}

