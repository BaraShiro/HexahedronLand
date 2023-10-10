using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(BiomeGenerator))]
public abstract class BiomeGeneratorInspector : Editor
{
    protected Texture2D previewTexture;
    
    private SerializedProperty biomeNameProp;
    private SerializedProperty biomeNoiseSettingsProp;
    private SerializedProperty initialLayerHandlerProp;
    private SerializedProperty fallbackLayerHandlerProp;
    private SerializedProperty useDomainWarpingProp;
    private SerializedProperty domainWarpingProp;
    private SerializedProperty vegetationProp;
 
    protected virtual void OnEnable()
    {
        previewTexture = PreviewTexturePainter.GetNewPreviewTexture();
        biomeNameProp = serializedObject.FindProperty("biomeName");
        biomeNoiseSettingsProp = serializedObject.FindProperty("biomeNoiseSettings");
        initialLayerHandlerProp = serializedObject.FindProperty("initialLayerHandler");
        fallbackLayerHandlerProp = serializedObject.FindProperty("fallbackLayerHandler");
        useDomainWarpingProp = serializedObject.FindProperty("useDomainWarping");
        domainWarpingProp = serializedObject.FindProperty("domainWarping");
        vegetationProp = serializedObject.FindProperty("vegetation");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(biomeNameProp, new GUIContent("Biome Name", 
            "The biome name must be unique as it is used as a key in the biome lookup table."));
        EditorGUILayout.PropertyField(biomeNoiseSettingsProp, new GUIContent("Noise Settings", 
            "Noise settings used to generate noise when generating the biome."));
        EditorGUILayout.PropertyField(initialLayerHandlerProp, new GUIContent("Initial Layer Handler", 
            "The first in a chain of handlers that try to place the blocks when generating the biome."));
        EditorGUILayout.PropertyField(fallbackLayerHandlerProp, new GUIContent("Fallback Layer Handler", 
            "The fallback layer handler places blocks when the other handlers fail to do so."));
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(useDomainWarpingProp, new GUIContent("Use Domain Warping", 
            "Should domain warping be used when generating the biome?"));
        if (useDomainWarpingProp.boolValue)
        {
            EditorGUILayout.PropertyField(domainWarpingProp, new GUIContent("Domain warping", 
                "Domain warping is used to break up the landscape to make it feel more natural."));   
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(vegetationProp, new GUIContent("Vegetation", 
            "The vegetation (trees, shrubs, grass, etc.) that will be used in this biome."));
        
        EditorGUILayout.Space();
        
        BiomeSpecificInspectorGUI();
        
        serializedObject.ApplyModifiedProperties();
    }

    protected abstract void BiomeSpecificInspectorGUI();
}

