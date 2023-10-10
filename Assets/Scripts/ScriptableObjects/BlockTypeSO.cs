using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "Block Type", menuName = "ScriptableObjects/Block Type", order = 1)]
public class BlockTypeSO : ScriptableObject
{
    public string blockTypeName;
    public Block.BlockData blockData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Block GetBlock()
    {
        if (!Block.BlockTypeExists(blockTypeName))
        {
            Block.TryAddNewBlockType(blockTypeName, blockData);
        }

        return Block.GetNewBlock(blockTypeName);
    }
}
