using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    bool initialized = false;
    Type[] inheritedTypes;
    string[] typePopupNameArray;
    string[] typeFullNameArray;
    int currentTypeIndex;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference) return;
        if(!initialized) {
            Initialize(property);
            initialized = true;
        }
        GetCurrentTypeIndex(property.managedReferenceFullTypename);
        int selectedTypeIndex = EditorGUI.Popup(GetPopupPosition(position), currentTypeIndex, typePopupNameArray);
        UpdatePropertyToSelectedTypeIndex(property, selectedTypeIndex);
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    private void Initialize(SerializedProperty property)
    {
        SubclassSelectorAttribute utility = (SubclassSelectorAttribute)attribute;
        GetAllInheritedTypes(GetFieldType(property), utility.IsIncludeMono());
        GetInheritedTypeNameArrays();
    }

    private void GetCurrentTypeIndex(string typeFullName)
    {
        currentTypeIndex = Array.IndexOf(typeFullNameArray, typeFullName);
    }

    private void GetAllInheritedTypes(Type baseType, bool includeMono)
    {
        Type monoType = typeof(MonoBehaviour);

        bool Predicate(Type type)
        {
            return baseType.IsAssignableFrom(type) &&
                   type.IsClass &&
                   !type.IsAbstract &&
                   (!monoType.IsAssignableFrom(type) || includeMono);
        }

        inheritedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(Predicate)
            // .Prepend(null)
            .ToArray();
    }

    private void GetInheritedTypeNameArrays()
    {
        typePopupNameArray = inheritedTypes.Select(type => type == null ? "<null>" : type.ToString()).ToArray();
        typeFullNameArray = inheritedTypes.Select(type => type == null ? "" : $"{type.Assembly.ToString().Split(',')[0]} {type.FullName}").ToArray();
    }

    public static Type GetFieldType(SerializedProperty property)
    {
        string[] fieldTypename = property.managedReferenceFieldTypename.Split(' ');
        var assembly = Assembly.Load(fieldTypename[0]);
        return assembly.GetType(fieldTypename[1]);
    }

    private void UpdatePropertyToSelectedTypeIndex(SerializedProperty property, int selectedTypeIndex)
    {
        if (currentTypeIndex == selectedTypeIndex) return;
        currentTypeIndex = selectedTypeIndex;
        Type selectedType = inheritedTypes[selectedTypeIndex];
        property.managedReferenceValue =
            selectedType == null ? null : Activator.CreateInstance(selectedType);
    }

    private Rect GetPopupPosition(Rect currentPosition)
    {
        Rect popupPosition = new Rect(currentPosition);
        popupPosition.width -= EditorGUIUtility.labelWidth;
        popupPosition.x += EditorGUIUtility.labelWidth;
        popupPosition.height = EditorGUIUtility.singleLineHeight;
        return popupPosition;
    }
}