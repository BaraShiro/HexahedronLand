using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public abstract class BlockLayerHandler : MonoBehaviour
{
    
    public readonly ref struct LayerData
    {
        public readonly int firstThreshold; // Rock
        public readonly int secondThreshold; // Surface
        public readonly Vector3Int worldPosition;
        
        public LayerData(int firstThreshold, int secondThreshold, Vector3Int worldPosition)
        {
            this.firstThreshold = firstThreshold;
            this.secondThreshold = secondThreshold;
            this.worldPosition = worldPosition;
        }
    }

    public BlockLayerHandler nextHandler;
    public BlockTypeSO blockType;

    public bool Handle(ref Chunk.ChunkData chunkData, in LayerData layerData)
    {
        if (TryHandle(in chunkData, in layerData, out Block block))
        {
            Chunk.SetBlockWorld(chunkData, layerData.worldPosition, block);
            return true;
        }
        else if(nextHandler)
        {
            return nextHandler.Handle(ref chunkData, in layerData);
        }
        else
        {
            return false;
        }
    }

    protected abstract bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block);
}
