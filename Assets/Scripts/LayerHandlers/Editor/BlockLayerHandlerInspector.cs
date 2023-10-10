using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(BlockLayerHandler))]
public class BlockLayerHandlerInspector : Editor
{
    protected SerializedProperty nextHandlerProp;
    protected SerializedProperty blockTypeProp;

    protected virtual void OnEnable()
    {
        nextHandlerProp = serializedObject.FindProperty("nextHandler");
        blockTypeProp = serializedObject.FindProperty("blockType");
    }

    protected virtual void BlockLayerHandlerPropertyFields()
    {
        EditorGUILayout.PropertyField(nextHandlerProp, new GUIContent("Next Layer Handler", 
            "The handler for the next layer, should be left empty on the last layer."));
        EditorGUILayout.PropertyField(blockTypeProp, new GUIContent("Block Type", 
            "The type of block to place in this layer."));
    }
}

