using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class BiomeGenerator : MonoBehaviour
{
    public string biomeName;
    public NoiseSettings biomeNoiseSettings;
    public BlockLayerHandler initialLayerHandler;
    public BlockLayerHandler fallbackLayerHandler;
    public bool useDomainWarping = false;
    public DomainWarping domainWarping;
    
    public VegetationSO vegetation;

    public abstract Chunk.ChunkData GenerateChunkColumn(Chunk.ChunkData chunkData, int worldPositionX, int worldPositionZ, int? firstThreshold, int? secondThreshold);
}
