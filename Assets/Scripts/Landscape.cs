using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Landscape
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3Int GetBlockPos(Vector3 pos)
    {
        Vector3Int blockPos = new Vector3Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z)
        );
  
        return blockPos;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3Int GetBlockPos(in RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
        );
  
        // Debug.Log($"GetBlock: {hit.point} {pos}");
        return GetBlockPos(pos);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        // TODO: Rewrite this
        // Debug.Log($"MoveWithin: {pos} {pos - (int)pos}");
        if (pos - (int)pos == 0.5f || pos - (int)pos == -0.5f)
        {
            if (adjacent)
            {
                pos += (norm / 2);
            }
            else
            {
                pos -= (norm / 2);
            }
        }
  
        return (float)pos;
    }
    
    public static bool SetBlock(in RaycastHit hit, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return false;
  
        Vector3Int pos = GetBlockPos(hit, adjacent);
  
        chunk.Data.world.SetBlock(pos.x, pos.y, pos.z, block);
  
        return true;
    }
    
    public static Block GetBlock(in RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return null;
  
        Vector3Int pos = GetBlockPos(hit, adjacent);
  
        Block block = chunk.Data.world.GetBlock(pos.x, pos.y, pos.z);
  
        return block;
    }

    public static void MineBlock(in RaycastHit hit)
    {
        // TODO: Move logic and use get/set block instead
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
        {
            return;
        }
        
        Vector3Int pos = GetBlockPos(hit);
        Block block = chunk.Data.world.GetBlock(pos.x, pos.y, pos.z);
        
        int blockHealth = block.Damage(1);

        if (blockHealth <= 0)
        {
            // PlayerController.AddToInventory(block.resource, block.resourceAmount);
            block.OnBlockDestroyed(EventArgs.Empty);
            chunk.Data.world.SetBlock(pos.x, pos.y, pos.z, block.ReplaceWith());
        }
        
    }

}
