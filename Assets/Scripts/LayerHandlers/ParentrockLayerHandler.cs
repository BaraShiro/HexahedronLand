using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentrockLayerHandler : BlockLayerHandler
{
    public int layerThickness = 1;
    protected override bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block)
    {
        int parentrockHeight = layerData.firstThreshold + layerThickness;
        if (layerData.worldPosition.y <= parentrockHeight && parentrockHeight < layerData.secondThreshold)
        {
            block = blockType.GetBlock();
            return true;
        }
        else
        {
            block = null;
            return false;
        }
    }
}