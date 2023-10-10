using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Decoration", menuName = "ScriptableObjects/Vegetation", order = 1)]

public class VegetationSO : ScriptableObject
{
    public List<Vegetation> treeLayer = new List<Vegetation>();
    public List<Vegetation> shrubLayer = new List<Vegetation>();
    public List<Vegetation> herbLayer = new List<Vegetation>();

    public Vegetation GetVegetation(Vegetation.VegetationType type, int x, int z)
    {
        return type switch
        {
            Vegetation.VegetationType.Tree => GetVegetation(treeLayer, x, z),
            Vegetation.VegetationType.Shrub => GetVegetation(shrubLayer, x, z),
            Vegetation.VegetationType.Herb => GetVegetation(herbLayer, x, z),
            _ => null
        };
    }

    private Vegetation GetVegetation(List<Vegetation> vegetationLayer, int x, int z)
    {
        return vegetationLayer.Count < 1 ? null : vegetationLayer[(x + 1 * z + 1) % vegetationLayer.Count];
    }
}
