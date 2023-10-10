using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirLayerHandler : BlockLayerHandler
{
    protected override bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block)
    {
        if (layerData.worldPosition.y > layerData.secondThreshold)
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
