using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.CompilerServices;

public static class Serialization
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SaveLocation(Vector3Int chunkLocation)
    {
        string saveLocation = Path.Combine(World.Instance.persistentDataPath, World.Instance.worldName);

        if (!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation);
        }

        saveLocation = Path.Combine(saveLocation, $"{chunkLocation.x},{chunkLocation.y},{chunkLocation.z}.chunk");
        
        return saveLocation;
    }

    public static bool SaveChunk(in Chunk.ChunkData chunkData)
    {
        SavedChunk savedChunk = new SavedChunk(in chunkData);
        if (savedChunk.blocks.Count == 0)
        {
            return true; // Nothing has changed, so nothing to save
        }

        // TODO:  GetInvalidPathChars()
        string saveFilePath = SaveLocation(chunkData.worldPosition);
        
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
        string saveFilePath = SaveLocation(chunkData.worldPosition);

        if (!File.Exists(saveFilePath))
        {
            return true; // Nothing has been saved, so nothing to load
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