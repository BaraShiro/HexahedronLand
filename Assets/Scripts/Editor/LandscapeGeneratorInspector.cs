using Cinemachine.Editor;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[CustomEditor(typeof(LandscapeGenerator))]
public class LandscapeGeneratorInspector : Editor
{
    private NoiseSettings temperatureNoiseSettings;
    private NoiseSettings precipitationNoiseSettings;
    
    private SerializedProperty biomeTemperatureNoiseSettingsProp;
    private SerializedProperty biomePrecipitationNoiseSettingsProp;
    private SerializedProperty biomeDomainWarpingProp;
    private SerializedProperty biomeClimateDataProp;
    private SerializedProperty useDomainWarpingProp;
    
    private Texture2D previewTexture;

    public void OnEnable()
    {
        biomeTemperatureNoiseSettingsProp = serializedObject.FindProperty("biomeTemperatureNoiseSettings");
        biomePrecipitationNoiseSettingsProp = serializedObject.FindProperty("biomePrecipitationNoiseSettings");
        biomeDomainWarpingProp = serializedObject.FindProperty("biomeDomainWarping");
        biomeClimateDataProp = serializedObject.FindProperty("biomeClimateData");
        useDomainWarpingProp = serializedObject.FindProperty("useDomainWarping");
        
        temperatureNoiseSettings = biomeTemperatureNoiseSettingsProp.objectReferenceValue as NoiseSettings;
        precipitationNoiseSettings = biomePrecipitationNoiseSettingsProp.objectReferenceValue as NoiseSettings;
        
        previewTexture = PreviewTexturePainter.GetNewPreviewTexture();

        if (biomeTemperatureNoiseSettingsProp.objectReferenceValue && biomePrecipitationNoiseSettingsProp.objectReferenceValue)
        {
            UpdatePreviewImage();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(biomeTemperatureNoiseSettingsProp, new GUIContent("Temperature noise settings", 
            "The noise settings for generating temperature."));
        EditorGUILayout.PropertyField(biomePrecipitationNoiseSettingsProp, new GUIContent("Precipitation noise settings", 
            "The noise settings for generating precipitation levels."));
        
        if(biomeTemperatureNoiseSettingsProp.objectReferenceValue && biomePrecipitationNoiseSettingsProp.objectReferenceValue)
        {
            PreviewImage();
        }
        else
        {
            EditorGUILayout.HelpBox("Can't generate preview! Select or create new Noise Settings.", 
                MessageType.Error, true);
        }
        
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(useDomainWarpingProp, new GUIContent("Use domain warping", 
            "Use domain warping to break up the borders between biomes."));
        EditorGUILayout.PropertyField(biomeDomainWarpingProp, new GUIContent("Domain warping", 
            "The domain warping for the biome center points."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(biomeClimateDataProp, new GUIContent("Biome climate data", 
            "A matrix of biomes with the level of precipitation on one axis and temperature on the other axis."));
        
        
        serializedObject.ApplyModifiedProperties();
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
        float temperatureNoise = PreviewTexturePainter.GetPixelValue(x, z, temperatureNoiseSettings);
        float precipitationNoise = PreviewTexturePainter.GetPixelValue(x, z, precipitationNoiseSettings);
        
        return new Color(temperatureNoise, 0f, precipitationNoise);
    }
}