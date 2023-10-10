using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resource", menuName = "ScriptableObjects/Resource", order = 1)]
public class Resource : ScriptableObject
{
    [SerializeField] private string resourceName;
    public string ResourceName => resourceName;
}
