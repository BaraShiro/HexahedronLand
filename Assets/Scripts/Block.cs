using UnityEngine;
using System;
using System.Collections.Concurrent;
using static Chunk;

[Serializable]
public class Block
{
    public enum Direction
    {
        North,
        East,
        South,
        West,
        Up,
        Down
    }

    [Serializable]
    public struct TextureTilePosition
    {
        public int x;
        public int y;

        public TextureTilePosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Serializable]
    public struct BlockData
    {
        public TextureTilePosition northTexturePosition;
        public TextureTilePosition eastTexturePosition;
        public TextureTilePosition southTexturePosition;
        public TextureTilePosition westTexturePosition;
        public TextureTilePosition upTexturePosition;
        public TextureTilePosition downTexturePosition;

        public bool northIsSolid;
        public bool eastIsSolid;
        public bool southIsSolid;
        public bool westIsSolid;
        public bool upIsSolid;
        public bool downIsSolid;

        public bool hasCollision;

        public int maxHealth;
        public BlockTypeSO replacementBlock;
        public string resource;
        public float resourceAmount;
        
    }

    public static readonly string DebugBlockName = "Debug";
    public static readonly string VoidBlockName = "Void";
    private static readonly TextureTilePosition DebugTilePosition = new TextureTilePosition(0, 0);
    private static readonly BlockData DebugBlockData = new BlockData()
    {
        northTexturePosition = DebugTilePosition,
        eastTexturePosition = DebugTilePosition,
        southTexturePosition = DebugTilePosition,
        westTexturePosition = DebugTilePosition,
        upTexturePosition = DebugTilePosition,
        downTexturePosition = DebugTilePosition,
        northIsSolid = true,
        eastIsSolid = true,
        southIsSolid = true,
        westIsSolid = true,
        upIsSolid = true,
        downIsSolid = true,
        hasCollision = true,
        maxHealth = 1,
        replacementBlock = null,
        resource = null,
        resourceAmount = 0f
    };
    private static readonly BlockData VoidBlockData = new BlockData()
    {
        northTexturePosition = DebugTilePosition,
        eastTexturePosition = DebugTilePosition,
        southTexturePosition = DebugTilePosition,
        westTexturePosition = DebugTilePosition,
        upTexturePosition = DebugTilePosition,
        downTexturePosition = DebugTilePosition,
        northIsSolid = false,
        eastIsSolid = false,
        southIsSolid = false,
        westIsSolid = false,
        upIsSolid = false,
        downIsSolid = false,
        hasCollision = false,
        maxHealth = 0,
        replacementBlock = null,
        resource = null,
        resourceAmount = 0f
    };

    private static readonly ConcurrentDictionary<string, BlockData> BlockTypes = new ConcurrentDictionary<string, BlockData>()
    {
        [DebugBlockName] = DebugBlockData,
        [VoidBlockName] = VoidBlockData
    };

    public string blockType;
    public int health = 1;
    
    [HideInInspector]
    public bool changed = false;

    public Vegetation.VegetationData? vegetation = null;

    public virtual event EventHandler BlockDestroyed;

    public virtual void OnBlockDestroyed(EventArgs e)
    {
        EventHandler handler = BlockDestroyed;

        handler?.Invoke(this, e);
    }

    public Block(string blockTypeName, bool changed = false)
    {
        this.blockType = blockTypeName;
        this.changed = changed;
        if (BlockTypes.TryGetValue(blockTypeName, out BlockData blockData))
        {
            health = blockData.maxHealth;
        }
        else
        {
            Debug.LogError($"Missing block type {blockTypeName}");
        }
    }

    public static bool BlockTypeExists(string blockTypeName)
    {
        return BlockTypes.ContainsKey(blockTypeName);
    }

    public static bool TryAddNewBlockType(string blockTypeName, BlockData blockData)
    {
        return BlockTypes.TryAdd(blockTypeName, blockData);
    }

    public static Block GetNewBlock(string blockTypeName)
    {
        if (BlockTypes.ContainsKey(blockTypeName))
        {
            return new Block(blockTypeName);
        }
        else
        {
            Debug.LogError($"Missing block type {blockTypeName}");
            return new Block(DebugBlockName);
        }
    }

    public virtual int Damage(int damageAmount)
    {
        health -= damageAmount;
        return health;
    }

    public virtual Block ReplaceWith()
    {
        if (BlockTypes.TryGetValue(blockType, out BlockData blockData))
        {
            if (blockData.replacementBlock)
            {
                return new Block(blockData.replacementBlock.blockTypeName, changed = true);   
            }
            else
            {
                Debug.LogError($"No replacement block for type {blockType} found");
                return new Block(DebugBlockName, changed = true);
            }
        }
        else
        {
            return new Block(DebugBlockName, changed = true);
        }
    }

    private TextureTilePosition TexturePosition(Direction direction)
    {
        if (BlockTypes.TryGetValue(blockType, out BlockData blockData))
        {
            return direction switch
            {
                Direction.North => blockData.northTexturePosition,
                Direction.East => blockData.eastTexturePosition,
                Direction.South => blockData.southTexturePosition,
                Direction.West => blockData.westTexturePosition,
                Direction.Up => blockData.upTexturePosition,
                Direction.Down => blockData.downTexturePosition,
                _ => DebugTilePosition
            };
        }
        else
        {
            Debug.LogError($"Missing block type {blockType}");
            return DebugTilePosition;
        }
    }
    
    private Vector2[] FaceUVs(Direction direction)
    {
        Vector2[] UVs = new Vector2[4];
        TextureTilePosition textureTilePosition = TexturePosition(direction);

        UVs[0] = new Vector2(TileSize * textureTilePosition.x + Offset, TileSize * textureTilePosition.y + Offset); // LowerLeft
        UVs[1] = new Vector2(TileSize * textureTilePosition.x + Offset, TileSize * textureTilePosition.y + TileSize - Offset); // UpperLeft
        UVs[2] = new Vector2(TileSize * textureTilePosition.x + TileSize - Offset, TileSize * textureTilePosition.y + TileSize - Offset); // UpperRight
        UVs[3] = new Vector2(TileSize * textureTilePosition.x + TileSize - Offset, TileSize * textureTilePosition.y + Offset); // LowerRight

        return UVs;
    }

    public void AddFaceDataToMeshData(ChunkData chunkData, int x, int y, int z, ref MeshData meshData)
    {
        if(!BlockTypes.TryGetValue(blockType, out BlockData blockData))
        {
            return;
        }

        if (blockData.southIsSolid && !GetBlock(chunkData, x, y, z - 1).IsSolid(Direction.North))
        {
            AddFaceDataSouth(x, y, z, blockData.hasCollision, ref meshData);
        }

        if (blockData.westIsSolid && !GetBlock(chunkData, x - 1, y, z).IsSolid(Direction.East))
        {
            AddFaceDataWest(x, y, z, blockData.hasCollision, ref meshData);
        }

        if (blockData.northIsSolid && !GetBlock(chunkData, x, y, z + 1).IsSolid(Direction.South))
        {
            AddFaceDataNorth(x, y, z, blockData.hasCollision, ref meshData);
        }

        if (blockData.eastIsSolid && !GetBlock(chunkData, x + 1, y, z).IsSolid(Direction.West))
        {
            AddFaceDataEast(x, y, z, blockData.hasCollision, ref meshData);
        }

        if (blockData.downIsSolid && !GetBlock(chunkData, x, y - 1, z).IsSolid(Direction.Up))
        {
            AddFaceDataDown(x, y, z, blockData.hasCollision, ref meshData);
        }

        if (blockData.upIsSolid && !GetBlock(chunkData, x, y + 1, z).IsSolid(Direction.Down))
        {
            AddFaceDataUp(x, y, z, blockData.hasCollision, ref meshData);
        }

        
    }
    
    private void AddFaceDataNorth (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
            FaceUVs(Direction.North),
            hasCollision
        );
    }

    private void AddFaceDataEast (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
            FaceUVs(Direction.East),
            hasCollision
        );
    }

    private void AddFaceDataSouth (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
            new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
            FaceUVs(Direction.South),
            hasCollision
        );
    }

    private void AddFaceDataWest (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
            new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
            new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
            FaceUVs(Direction.West),
            hasCollision
        );
    }

    private void AddFaceDataUp (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
            new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
            new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
            FaceUVs(Direction.Up),
            hasCollision
        );
    }
  
    private void AddFaceDataDown (int x, int y, int z, bool hasCollision, ref MeshData meshData)
    {
        meshData.AddQuad
        (
            new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
            new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
            new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
            FaceUVs(Direction.Down),
            hasCollision
        );
    }

    public bool IsSolid(Direction direction)
    {
        if (BlockTypes.TryGetValue(blockType, out BlockData blockData))
        {
            return direction switch
            {
                Direction.North => blockData.northIsSolid,
                Direction.East => blockData.eastIsSolid,
                Direction.South => blockData.southIsSolid,
                Direction.West => blockData.westIsSolid,
                Direction.Up => blockData.upIsSolid,
                Direction.Down => blockData.downIsSolid,
                _ => false
            };   
        }
        else
        {
            Debug.LogError($"Missing block type {blockType}");
            return false;
        }
    }
}
