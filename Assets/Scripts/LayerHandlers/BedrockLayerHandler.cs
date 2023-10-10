using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedrockLayerHandler : BlockLayerHandler
{
    protected override bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block)
    {
        if (layerData.worldPosition.y <= layerData.firstThreshold)
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
