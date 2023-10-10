// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;
//
// [CustomEditor(typeof(BlockTypeSO))]
// class BlockTypeSOInspector : Editor
// {
//     private Type[] _implementations;
//     private int _implementationTypeIndex;
//
//     public override void OnInspectorGUI()
//     {
//         BlockTypeSO blockTypeSO = target as BlockTypeSO;
//         //specify type
//         if (blockTypeSO == null)
//         {
//             return;
//         }
//         
//         if (_implementations == null || GUILayout.Button("Refresh implementations"))
//         {
//             //this is probably the most imporant part:
//             //find all implementations of INode using System.Reflection.Module
//             _implementations = GetImplementations<Block>().Where(type => !type.IsSubclassOf(typeof(UnityEngine.Object))).ToArray();
//         }
//         
//         EditorGUILayout.LabelField($"Found {_implementations.Count()} implementations ");
//         
//         //select implementation from editor popup
//         _implementationTypeIndex = EditorGUILayout.Popup(new GUIContent("Implementation"),
//             _implementationTypeIndex, _implementations.Select(type => type.FullName).ToArray());
//         
//         
//         if (GUILayout.Button("Set block type"))
//         {
//             //set new value
//             Debug.Log(_implementations[_implementationTypeIndex]);
//             
//             blockTypeSO.blockType = _implementations[_implementationTypeIndex];
//             // testBehaviour.Node = (INode) Activator.CreateInstance(_implementations[_implementationTypeIndex]);
//         }
//         
//         try
//         {
//             if (blockTypeSO.blockType != null)
//             {
//                 // EditorGUILayout.LabelField($"BlockType is {blockTypeSO.blockType.FullName} ", new GUIStyle {fontSize = 20});
//                 EditorGUILayout.LabelField($"BlockType is set ", new GUIStyle {fontSize = 20});
//             }
//             else
//             {
//                 EditorGUILayout.LabelField($"BlockType not chosen!", new GUIStyle {fontSize = 20});
//             }
//         
//         }
//         catch (Exception e)
//         {
//             Debug.Log(e);
//             return;
//         }
//
//         
//         base.OnInspectorGUI();
//     }
//     
//     private static Type[] GetImplementations<T>()
//     {
//         IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
//     
//         Type baseType = typeof(T);
//         return types.Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract).ToArray();
//     }
// }
//
