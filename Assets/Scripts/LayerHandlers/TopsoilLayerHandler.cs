using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class TopsoilLayerHandler : BlockLayerHandler
{
    public NoiseSettings surfaceNoiseSettings;
    public SurfaceBiomeGenerator surfaceBiomeGenerator;

    [Range(0f,1f)]
    public float treeLayerDensity = 0.05f;
    [Range(0f,1f)]
    public float shrubLayerDensity = 0.1f;
    [Range(0f,1f)]
    public float herbLayerDensity = 0.2f;

    protected override bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block)
    {
        if (layerData.worldPosition.y == layerData.secondThreshold)
        {
            block = blockType.GetBlock();
            float noise = NoiseGenerator.OctaveSimplexNoiseBurstCompiled(layerData.worldPosition.x + World.WorldOffset.x, layerData.worldPosition.z + World.WorldOffset.z, surfaceNoiseSettings.settingsData);
            if (surfaceNoiseSettings.useEasing)
            {
                noise = NoiseGenerator.Redistribution(noise, surfaceNoiseSettings.redistributionModifier, surfaceNoiseSettings.exponent);
                // Func<float, float> EasingFunction = Easing.GetEasingFunction(surfaceNoiseSettings.easingFunction);
                // noise = EasingFunction(noise * surfaceNoiseSettings.redistributionModifier);
            }
            
            if (noise <= treeLayerDensity)
            {
                block.health = 4;
                block.vegetation = new Vegetation.VegetationData(surfaceBiomeGenerator.biomeName, Vegetation.VegetationType.Tree);
                // Debug.Log($"Noise: {noise} -> Tree");
            }
            else if (noise <= shrubLayerDensity)
            {
                block.health = 2;
                block.vegetation = new Vegetation.VegetationData(surfaceBiomeGenerator.biomeName, Vegetation.VegetationType.Shrub);
                // Debug.Log($"Noise: {noise} -> Bush");
            }
            else if(noise <= herbLayerDensity)
            {
                block.vegetation = new Vegetation.VegetationData(surfaceBiomeGenerator.biomeName, Vegetation.VegetationType.Herb);
                // Debug.Log($"Noise: {noise} -> Grass");
            }
            else
            {
                // Debug.Log($"Noise: {noise}");
            }

            return true;
        }
        else
        {
            block = null;
            return false;
        }
    }
}
