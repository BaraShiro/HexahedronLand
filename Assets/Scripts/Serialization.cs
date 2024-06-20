using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.CompilerServices;

public static class Serialization
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetSaveFilePath(Vector3Int chunkPosition)
    {
        string validWorldName = World.Instance.worldName.ReplaceInvalidPathChars("_");
        string savePath = Path.Combine(World.Instance.persistentDataPath, validWorldName);

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string fullSavePath = Path.Combine(savePath, $"{chunkPosition.x},{chunkPosition.y},{chunkPosition.z}.chunk");
        
        return fullSavePath;
    }

    public static bool SaveChunk(in Chunk.ChunkData chunkData)
    {
        SavedChunk savedChunk = new SavedChunk(in chunkData);
        if (savedChunk.blocks.Count == 0)
        {
            // Nothing has changed, so nothing to save
            return true;
        }

        string saveFilePath = GetSaveFilePath(chunkData.worldPosition);
        
        try
        {
            File.WriteAllText(saveFilePath, savedChunk.ToJson());
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
    
    public static bool LoadChunk(ref Chunk.ChunkData chunkData)
    {
        string saveFilePath = GetSaveFilePath(chunkData.worldPosition);

        if (!File.Exists(saveFilePath))
        {
            // Nothing has been saved, so nothing to load
            return true;
        }

        SavedChunk savedChunk;
        try
        {
            savedChunk = new SavedChunk(File.ReadAllText(saveFilePath));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }

        if (!savedChunk.AssertData())
        {
            Debug.LogWarning($"Saved chunk for position {chunkData.worldPosition} contained faulty block data, disregarding.");
        }

        foreach (SavedChunk.SavedBlock savedBlock in savedChunk.blocks)
        {
            chunkData.blocks[savedBlock.x][savedBlock.z][savedBlock.y] = savedBlock.block;
        }

        return true;
    }
}