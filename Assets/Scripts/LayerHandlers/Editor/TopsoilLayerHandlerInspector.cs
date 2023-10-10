using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
[CustomEditor(typeof(TopsoilLayerHandler))]
public class TopsoilLayerHandlerInspector : BlockLayerHandlerInspector
{
    private TopsoilLayerHandler topsoilLayerHandler;
    private const int IconTextureSize = 18;
    private Texture2D previewTexture;
    private Texture2D treeIconTexture;
    private Texture2D bushIconTexture;
    private Texture2D grassIconTexture;
    
    private static readonly Color TreeColor = Color.red;
    private static readonly Color BushColor = Color.yellow;
    private static readonly Color GrassColor = Color.green;
    private static readonly Color BackgroundColor = Color.gray;

    private SerializedProperty surfaceNoiseSettingsProp;
    private SerializedProperty biomeGeneratorProp;
    
    private SerializedProperty treeLayerDensityProp;
    private SerializedProperty shrubLayerDensityProp;
    private SerializedProperty herbLayerDensityProp;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        surfaceNoiseSettingsProp = serializedObject.FindProperty("surfaceNoiseSettings");
        biomeGeneratorProp = serializedObject.FindProperty("surfaceBiomeGenerator");
        treeLayerDensityProp = serializedObject.FindProperty ("treeLayerDensity");
        shrubLayerDensityProp = serializedObject.FindProperty("shrubLayerDensity");
        herbLayerDensityProp = serializedObject.FindProperty("herbLayerDensity");
        
        topsoilLayerHandler = serializedObject.targetObject as TopsoilLayerHandler;
        
        previewTexture = PreviewTexturePainter.GetNewPreviewTexture();

        GenerateIconTextures();

        if(topsoilLayerHandler && topsoilLayerHandler.surfaceNoiseSettings)
        {
            UpdatePreviewImage();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
            
        EditorGUILayout.HelpBox("Topsoil is the upper layer of soil, and is where plants grow.", 
            MessageType.Info, true);
        
        EditorGUILayout.Space();
        
        BlockLayerHandlerPropertyFields();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(surfaceNoiseSettingsProp, new GUIContent("Surface Noise Settings", "The noise settings used to distribute trees, bushes, and grass."));
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(biomeGeneratorProp, new GUIContent("Biome Generator", "The biome generator used to to generate the trees, bushes, and grass."));

        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("The tree layer always override the shrub layer, and the shrub layer always override the herb layer.", 
            MessageType.Info, false);
        
        EditorGUILayout.Space();

        EditorGUILayout.Slider(treeLayerDensityProp, 0, 1,
            new GUIContent("Tree Layer Density", treeIconTexture, 
                "The density of trees placed on the terrain."));
        if (treeLayerDensityProp.floatValue > shrubLayerDensityProp.floatValue)
        {
            shrubLayerDensityProp.floatValue = treeLayerDensityProp.floatValue;
        }

        EditorGUILayout.Slider(shrubLayerDensityProp, treeLayerDensityProp.floatValue, 1,
            new GUIContent("Shrub Layer Density", bushIconTexture,
                "The density of bushes and shrubs placed on the terrain. Cannot be lower than the density of trees."));
        if (shrubLayerDensityProp.floatValue > herbLayerDensityProp.floatValue)
        {
            herbLayerDensityProp.floatValue = shrubLayerDensityProp.floatValue;
        }

        EditorGUILayout.Slider(herbLayerDensityProp, shrubLayerDensityProp.floatValue, 1,
            new GUIContent("Herb Layer Density", grassIconTexture,
                "The density of herbaceous plants and grass placed on the terrain. Cannot be lower than the density of bushes or trees."));
        
        EditorGUILayout.Space();
        
        if(topsoilLayerHandler.surfaceNoiseSettings)
        {
            PreviewImage();
        }
        else
        {
            EditorGUILayout.HelpBox("Can't generate preview! Select or create new Noise Settings.", 
                MessageType.Error, true);
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    private void GenerateIconTextures()
    {
        treeIconTexture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false);
        bushIconTexture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false);
        grassIconTexture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false);
        
        Color[] treeIconTextureColorArray = treeIconTexture.GetPixels();
        Color[] bushIconTextureColorArray = bushIconTexture.GetPixels();
        Color[] grassIconTextureColorArray = grassIconTexture.GetPixels();
        for (int i = 0; i < IconTextureSize * IconTextureSize; i++)
        {
            treeIconTextureColorArray[i] = TreeColor;
            bushIconTextureColorArray[i] = BushColor;
            grassIconTextureColorArray[i] = GrassColor;
        }

        treeIconTexture.SetPixels(treeIconTextureColorArray);
        treeIconTexture.Apply();
        bushIconTexture.SetPixels(bushIconTextureColorArray);
        bushIconTexture.Apply();
        grassIconTexture.SetPixels(grassIconTextureColorArray);
        grassIconTexture.Apply();
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
                previewTexture.SetPixel(x, z, SetPixelColor(x, z));
            }
        }
        previewTexture.Apply();
    }

    private Color SetPixelColor(int x, int z)
    {
        float noise = PreviewTexturePainter.GetPixelValue(x, z, topsoilLayerHandler.surfaceNoiseSettings);
        if (noise <= topsoilLayerHandler.treeLayerDensity)
        {
            return TreeColor;
        }
        else if (noise <= topsoilLayerHandler.shrubLayerDensity)
        {
            return BushColor;
        }
        else if(noise <= topsoilLayerHandler.herbLayerDensity)
        {
            return GrassColor;
        }
        else
        {
            return BackgroundColor;
        }
    }
    
}

