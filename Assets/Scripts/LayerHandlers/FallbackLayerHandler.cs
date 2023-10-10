using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallbackLayerHandler : BlockLayerHandler
{
    protected override bool TryHandle(in Chunk.ChunkData chunkData, in LayerData layerData, out Block block)
    {
        block = blockType.GetBlock();
        return true;
    }
}