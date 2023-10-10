using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceBiomeGenerator : BiomeGenerator
{
    public int rockMinHeight = 0;
    public int rockMaxHeight = 32;
    public int dirtMinHeight = 2;
    public int dirtMaxHeight = 5;
    
    public override Chunk.ChunkData GenerateChunkColumn(Chunk.ChunkData chunkData, int worldPositionX, int worldPositionZ, int? firstThreshold, int? secondThreshold)
    {
        int rockHeight;
        int dirtOffset;
        if (firstThreshold.HasValue && secondThreshold.HasValue)
        {
            rockHeight = firstThreshold.Value;
            dirtOffset = secondThreshold.Value;
        }
        else
        {
            (rockHeight, dirtOffset) = GetSurfaceHeightNoise(worldPositionX + World.WorldOffset.x, worldPositionZ + World.WorldOffset.z);
        }

        int surfaceHeight = rockHeight + dirtOffset;
        
        for (int y = chunkData.worldPosition.y; y < chunkData.worldPosition.y + Chunk.ChunkData.ChunkSize; y++)
        {
            BlockLayerHandler.LayerData layerData = new BlockLayerHandler.LayerData(
                rockHeight, 
                surfaceHeight, 
                new Vector3Int(worldPositionX, y, worldPositionZ));

            bool layerHandled = initialLayerHandler.Handle(ref chunkData, in layerData);
            
            if(!layerHandled)
            {
                fallbackLayerHandler.Handle(ref chunkData, in layerData);
            }

        }

        return chunkData;
    }

    public (int rockHeight, int dirtHeight) GetSurfaceHeightNoise(int x, int z)
    {
        float landscapeHeight;
        
        if (useDomainWarping && domainWarping)
        {
            landscapeHeight = domainWarping.GenerateDomainNoise(x, z, biomeNoiseSettings);
        }
        else
        {
            landscapeHeight = NoiseGenerator.OctaveSimplexNoiseBurstCompiled(x, z, biomeNoiseSettings.settingsData);
        }
        
        if (biomeNoiseSettings.useEasing)
        {
            landscapeHeight = NoiseGenerator.Redistribution(landscapeHeight, biomeNoiseSettings.redistributionModifier, biomeNoiseSettings.exponent);
            // Func<float, float> EasingFunction = Easing.GetEasingFunction(biomeNoiseSettings.easingFunction);
            // landscapeHeight = EasingFunction(landscapeHeight * biomeNoiseSettings.redistributionModifier);
        }
        
        int rockHeight = NoiseGenerator.RemapUnitIntervalToInt(landscapeHeight, rockMinHeight, rockMaxHeight);
        int dirtHeight = NoiseGenerator.RemapUnitIntervalToInt(landscapeHeight, dirtMinHeight, dirtMaxHeight);
        return (rockHeight, dirtHeight);
    }
}
