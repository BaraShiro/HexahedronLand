using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Vegetation : MonoBehaviour
{
    public enum VegetationType
    {
        Herb = 0,
        Shrub = 1,
        Tree = 2
    }
    
    [Serializable]
    public readonly struct VegetationData
    {
        public readonly string biome;
        public readonly VegetationType type;

        public VegetationData(string biome, VegetationType type)
        {
            this.biome = biome;
            this.type = type;
        }
    }
    
    private Block parentBlock = null;

    public Block ParentBlock
    {
        get => parentBlock;
        set
        {
            parentBlock = value;
            parentBlock.BlockDestroyed += ParentBlockDestroyed;
        }
    }

    public virtual void Destroy()
    {
        parentBlock.BlockDestroyed -= ParentBlockDestroyed;
    }

    protected abstract void ParentBlockDestroyed(object sender, EventArgs e);
}
