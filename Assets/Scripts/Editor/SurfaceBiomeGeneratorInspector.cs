using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(SurfaceBiomeGenerator))]
public class SurfaceBiomeGeneratorInspector : BiomeGeneratorInspector
{
    private SurfaceBiomeGenerator surfaceBiomeGenerator;
    
    private SerializedProperty rockMinHeightProp;
    private SerializedProperty rockMaxHeightProp;
    private SerializedProperty dirtMinHeightProp;
    private SerializedProperty dirtMaxHeightProp;

    private float rockMinFloat;
    private float rockMaxFloat;
    private float dirtMinFloat;
    private float dirtMaxFloat;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        surfaceBiomeGenerator = serializedObject.targetObject as SurfaceBiomeGenerator;
        
        rockMinHeightProp = serializedObject.FindProperty("rockMinHeight");
        rockMaxHeightProp = serializedObject.FindProperty("rockMaxHeight");
        dirtMinHeightProp = serializedObject.FindProperty("dirtMinHeight");
        dirtMaxHeightProp = serializedObject.FindProperty("dirtMaxHeight");

        if (surfaceBiomeGenerator && surfaceBiomeGenerator.biomeNoiseSettings)
        {
            UpdatePreviewImage();
        }
    }
    protected override void BiomeSpecificInspectorGUI()
    {
        rockMinFloat = rockMinHeightProp.intValue;
        rockMaxFloat = rockMaxHeightProp.intValue;

        EditorGUILayout.MinMaxSlider(
            new GUIContent("Rock height", "The elevation of the rock layer."), 
            ref rockMinFloat, ref rockMaxFloat, 
            Mathf.Min(0f, rockMinFloat), Mathf.Max(100f, rockMaxFloat));
        
        if (rockMinFloat > rockMaxFloat) rockMinFloat = rockMaxFloat;
        if (rockMaxFloat < rockMinFloat) rockMaxFloat = rockMinFloat;
        
        rockMinHeightProp.intValue = Mathf.RoundToInt(rockMinFloat);
        rockMaxHeightProp.intValue = Mathf.RoundToInt(rockMaxFloat);

        EditorGUILayout.PropertyField(rockMinHeightProp,
            new GUIContent("Minimum rock level", "The lowest elevation of the rock layer."));
        EditorGUILayout.PropertyField(rockMaxHeightProp, 
            new GUIContent("Maximum rock level", "The highest elevation of the rock layer."));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField(
            new GUIContent("Rock level range", "The range in elevation of the rock layer"), 
            Mathf.RoundToInt(rockMaxFloat) - Mathf.RoundToInt(rockMinFloat));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        
        dirtMinFloat = dirtMinHeightProp.intValue;
        dirtMaxFloat = dirtMaxHeightProp.intValue;

        EditorGUILayout.MinMaxSlider(
            new GUIContent("Dirt height", "The thickness of the dirt layer."), 
            ref dirtMinFloat, ref dirtMaxFloat, 
            Mathf.Min(0f, dirtMinFloat), Mathf.Max(50f, dirtMaxFloat));
        
        if (dirtMinFloat > dirtMaxFloat) dirtMinFloat = dirtMaxFloat;
        if (dirtMaxFloat < dirtMinFloat) dirtMaxFloat = dirtMinFloat;
        
        dirtMinHeightProp.intValue = Mathf.RoundToInt(dirtMinFloat);
        dirtMaxHeightProp.intValue = Mathf.RoundToInt(dirtMaxFloat);

        EditorGUILayout.PropertyField(dirtMinHeightProp,
            new GUIContent("Minimum dirt level", "The lowest thickness of the dirt layer."));
        EditorGUILayout.PropertyField(dirtMaxHeightProp,
            new GUIContent("Maximum dirt level", "The highest thickness of the dirt layer."));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField(
            new GUIContent("Dirt level range", "The range in thickness of the dirt layer"), 
            Mathf.RoundToInt(dirtMaxFloat) - Mathf.RoundToInt(dirtMinFloat));
        EditorGUI.EndDisabledGroup();
        
        serializedObject.ApplyModifiedProperties();
                
        EditorGUILayout.Space();
        
        if(surfaceBiomeGenerator.biomeNoiseSettings)
        {
            PreviewImage();
        }
        else
        {
            EditorGUILayout.HelpBox("Can't generate preview! Select or create new Noise Settings.", 
                MessageType.Error, true);
        }

        
    }
    
    private void PreviewImage()
    {
        if(GUI.changed || serializedObject.hasModifiedProperties)
        {
            UpdatePreviewImage();
        }

        Rect rect = GUILayoutUtility.GetRect(PreviewTexturePainter.PreviewTextureSize, PreviewTexturePainter.PreviewTextureSize, "TextArea");
        EditorGUI.DrawPreviewTexture(rect, previewTexture, null, ScaleMode.ScaleAndCrop);
    }
    
    private void UpdatePreviewImage()
    {
        for (int x = 0; x < previewTexture.height; x++)
        {
            for (int z = 0; z < previewTexture.width; z++)
            {
                previewTexture.SetPixel(x, z, GetPixelColor(x, z));
            }
        }
        previewTexture.Apply();
    }

    private Color GetPixelColor(int x, int z)
    {
        // float noise = PreviewTexturePainter.GetPixelValue(x, z, surfaceBiomeGenerator.biomeNoiseSettings);

        int minHeight = rockMinHeightProp.intValue + dirtMinHeightProp.intValue;
        int maxHeight = rockMaxHeightProp.intValue + dirtMaxHeightProp.intValue;

        (int rockHeight, int dirtHeight) = surfaceBiomeGenerator.GetSurfaceHeightNoise(x, z);

        int surfaceHeight = rockHeight + dirtHeight;

        float normalizedNoise = NoiseGenerator.Normalize(surfaceHeight, minHeight, maxHeight);
        
        return new Color(normalizedNoise, normalizedNoise, normalizedNoise);

    }
}
