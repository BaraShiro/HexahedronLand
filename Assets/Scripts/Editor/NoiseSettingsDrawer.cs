using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

[CustomPropertyDrawer(typeof(NoiseSettings))]
public class NoiseSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        EditorGUILayout.PropertyField(property, new GUIContent("Noise settings", "The noise settings ScriptableObject to use when generating the noise."), false);
        if (GUI.changed || property.serializedObject.hasModifiedProperties) property.serializedObject.ApplyModifiedProperties();

        if (property.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            using (SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settingsData"), 
                    new GUIContent("Settings data", "The settings data that gets passed to the noise generator function."));
                
                EditorGUILayout.Space();

                SerializedProperty useEasingProp = serializedObject.FindProperty("useEasing");
                EditorGUILayout.PropertyField(useEasingProp, 
                    new GUIContent("Use easing function", "Easing functions control the rate of change of a value over time."));
                
                EditorGUI.BeginDisabledGroup(!useEasingProp.boolValue);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("redistributionModifier"), 
                    new GUIContent("Redistribution Modifier", "Fine-tunes the easing curve by multiplying the noise value with the redistribution modifier."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("exponent"), 
                    new GUIContent("Exponent", "Not used"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("easingFunction"),
                new GUIContent("Easing Function", "The easing function to use. In-functions create concave curves, Out-functions create convex curves, and In-Out-functions create S-shaped curves."));
                EditorGUI.EndDisabledGroup();
        
                if (GUI.changed || serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.indentLevel--;
            
        }
        else
        {
            
            if(GUILayout.Button("Create new noise settings"))
            {
                string selectedAssetPath = "Assets/Settings/NoiseSettings";
                property.objectReferenceValue = CreateNewNoiseSettings(selectedAssetPath);
                property.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI(); // Hack to prevent EditorGUI.EndProperty(); from throwing InvalidOperationException: Stack empty.
            }
        }
        
        if (GUI.changed || property.serializedObject.hasModifiedProperties)
        {
            property.serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUI.EndProperty();
    }
    
    private static ScriptableObject CreateNewNoiseSettings(string path)
    {
        path = EditorUtility.SaveFilePanelInProject("Save new noise settings", "NoiseSettings.asset", 
            "asset", "Enter a file name for the new noise settings ScriptableObject.", path);
        if (string.IsNullOrEmpty(path)) return null; // User pressed cancel
        
        ScriptableObject noiseSettingsAsset = ScriptableObject.CreateInstance(typeof(NoiseSettings));
        AssetDatabase.CreateAsset(noiseSettingsAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        EditorGUIUtility.PingObject(noiseSettingsAsset);
        
        return noiseSettingsAsset;
    }
    
}

[CustomPropertyDrawer(typeof(NoiseSettings.SettingsData))]
public class NoiseSettingsDataDrawer : PropertyDrawer
{
    private SerializedProperty offsetProp;
    private SerializedProperty offsetXProp;
    private SerializedProperty offsetYProp;
    private SerializedProperty offsetZProp;
    private Vector3Int offsetVector3Int = Vector3Int.zero;
    private SerializedProperty octavesProp;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        EditorGUI.indentLevel++;
        
        offsetProp = property.FindPropertyRelative("offset");
        offsetXProp = offsetProp.FindPropertyRelative("x");
        offsetYProp = offsetProp.FindPropertyRelative("y");
        offsetZProp = offsetProp.FindPropertyRelative("z");
        offsetVector3Int.x = offsetXProp.intValue;
        offsetVector3Int.y = offsetYProp.intValue;
        offsetVector3Int.z = offsetZProp.intValue;
        offsetVector3Int = EditorGUILayout.Vector3IntField(
            new GUIContent(
                "Offset", 
                "Offsets the coordinates used to generate noise.\nThe Y component is not used when computing 2D noise."), 
            offsetVector3Int);
        offsetXProp.intValue = offsetVector3Int.x;
        offsetYProp.intValue = offsetVector3Int.y;
        offsetZProp.intValue = offsetVector3Int.z;

        EditorGUILayout.Slider(property.FindPropertyRelative("noiseScale"), 0f, 1f,
            new GUIContent("Scale", "Controls at what scale the noise is computed by multiplying the scale value to the coordinates used to generate noise.\nThe noise repeats at integer values."));
        octavesProp = property.FindPropertyRelative("octaves");
        EditorGUILayout.IntSlider(octavesProp, 1, 10, 
            new GUIContent("Octaves", "Each octave adds finer detail to the overall noise by adding more noise with increased frequency and decreased amplitude.\nOctaves are performance sensitive as with each octave there is a linear increase in code execution time."));

        EditorGUI.BeginDisabledGroup(octavesProp.intValue <= 1);
    
        EditorGUILayout.Slider(property.FindPropertyRelative("persistence"), 0f, 1f,
            new GUIContent(
                "Persistence",
                "The persistence value determines how much influence each successive octave has over the final noise."));
        
        EditorGUI.EndDisabledGroup();
        
        if (GUI.changed || property.serializedObject.hasModifiedProperties)
        {
            property.serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUI.indentLevel--;
        
        
        EditorGUI.EndProperty();
    }
}