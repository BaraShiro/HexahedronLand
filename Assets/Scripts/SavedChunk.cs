using UnityEngine;
using System.Collections.Generic;
using System;
using static Chunk;
using static Chunk.ChunkData;
 
[Serializable]
public class SavedChunk
{
    [Serializable]
    public struct SavedBlock
    {
        public int x;
        public int y;
        public int z;
        public Block block;

        public SavedBlock(int x, int y, int z, Block block)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.block = block;
        }
    }
    
    public List<SavedBlock> blocks = new List<SavedBlock>();

    public SavedChunk(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
    }
    
    public SavedChunk(in ChunkData chunkData)
    {
        for (int x = 0; x < ChunkHorizontalSize; x++)
        {
            for (int y = 0; y < ChunkVerticalSize; y++)
            {
                for (int z = 0; z < ChunkHorizontalSize; z++)
                {
                    if (!chunkData.blocks[x, y, z].changed)
                    {
                        continue;
                    }
                    
                    blocks.Add(new SavedBlock(x, y, z, chunkData.blocks[x, y, z]));
                }
            }
        }
    }
    
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public bool AssertData()
    {
        int lengthBefore = blocks.Count;
        blocks.RemoveAll(savedBlock =>
            savedBlock.x is < 0 or >= ChunkHorizontalSize ||
            savedBlock.y is < 0 or >= ChunkVerticalSize ||
            savedBlock.z is < 0 or >= ChunkHorizontalSize ||
            string.IsNullOrEmpty(savedBlock.block.blockType)
            );

        return lengthBefore == blocks.Count;
    }
}