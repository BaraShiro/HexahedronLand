using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public class ChunkData
    {
        public Block[,,] blocks = new Block[ChunkHorizontalSize, ChunkVerticalSize, ChunkHorizontalSize];
        public const int ChunkHorizontalSize = 16;
        public const int ChunkVerticalSize = 64;
        public World world;
        public Vector3Int worldPosition;
        public ChunkData(World world, Vector3Int worldPosition)
        {
            this.world = world;
            this.worldPosition = worldPosition;
        }
    }
    
    public const float tileSize = 0.125f; // TODO: Make changeable in inspector somehow
    public const float offset = 0.001f; // Compensate for floating point rounding errors

    public ChunkData Data { get; private set; }
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    
    private void Awake()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
    }

    private void OnApplicationQuit()
    {
        Serialization.SaveChunk(this.Data);
    }
    
    public void InitializeChunk(ChunkData chunkData)
    {
        this.Data = chunkData;
    }

    public void UpdateChunk(MeshData meshData)
    {
        RenderMesh(meshData);
    }

    public void UpdateChunk()
    {
        MeshData meshData = CalculateMeshData(Data);
        RenderMesh(meshData);
    }

    private void RenderMesh(MeshData meshData)
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = meshData.GetVertices();
        meshFilter.mesh.triangles = meshData.GetTriangles();
        meshFilter.mesh.uv = meshData.GetUVs();
        
        meshFilter.mesh.RecalculateNormals();
        
        meshCollider.sharedMesh = null;
        Mesh colliderSharedMesh = new Mesh
        {
            vertices = meshData.GetColliderVertices(),
            triangles = meshData.GetColliderTriangles()
            
        };
        colliderSharedMesh.RecalculateNormals();
  
        meshCollider.sharedMesh = colliderSharedMesh;
    }
    
    
    #region Static functions
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IterateOverBlocks(ChunkData data, Action<int, int, int> action)
    {
        for (int x = 0; x < ChunkData.ChunkHorizontalSize; x++)
        {
            for (int y = 0; y < ChunkData.ChunkVerticalSize; y++)
            {
                for (int z = 0; z < ChunkData.ChunkHorizontalSize; z++)
                {
                    action(x, y, z);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IterateOverBlocks(ChunkData data, Action<Block> action)
    {
        foreach (Block block in data.blocks)
        {
            action(block);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InHorizontalRange(int index)
    {
        return index is >= 0 and < ChunkData.ChunkHorizontalSize;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InVerticalRange(int index)
    {
        return index is >= 0 and < ChunkData.ChunkVerticalSize;
    }

    //TODO: Make world space getters
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Block GetBlock(ChunkData chunkData, in Vector3Int localPosition)
    {
        return GetBlock(chunkData, localPosition.x, localPosition.y, localPosition.z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Block GetBlock(ChunkData chunkData, int localX, int localY, int localZ)
    {
        if(InHorizontalRange(localX) && InVerticalRange(localY) && InHorizontalRange(localZ))
        {
            return chunkData.blocks[localX, localY, localZ];
        }
        else
        {
            return chunkData.world.GetBlock(
                chunkData.worldPosition.x + localX, 
                chunkData.worldPosition.y + localY, 
                chunkData.worldPosition.z + localZ
                ); 
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBlockWorld(ChunkData chunkData, in Vector3Int worldPosition, Block block)
    {
        SetBlockLocal(chunkData, BlockWorldPosToLocalPos(chunkData, worldPosition), block);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBlockWorld(ChunkData chunkData, int worldX, int worldY, int worldZ, Block block)
    {
        SetBlockLocal(
            chunkData, 
            worldX - chunkData.worldPosition.x, 
            worldY - chunkData.worldPosition.y, 
            worldZ - chunkData.worldPosition.z, 
            block);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBlockLocal(ChunkData chunkData, in Vector3Int localPosition, Block block)
    {
        SetBlockLocal(chunkData, localPosition.x, localPosition.y, localPosition.z, block);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBlockLocal(ChunkData chunkData, int localX, int localY, int localZ, Block block)
    {
        if (InHorizontalRange(localX) && InVerticalRange(localY) && InHorizontalRange(localZ))
        {
            chunkData.blocks[localX, localY, localZ] = block;
        }
        else
        {
            chunkData.world.SetBlock(
                chunkData.worldPosition.x + localX, 
                chunkData.worldPosition.y + localY, 
                chunkData.worldPosition.z + localZ, 
                block);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int BlockWorldPosToLocalPos(ChunkData chunkData, in Vector3Int worldPos)
    {
        return new Vector3Int(
            worldPos.x - chunkData.worldPosition.x, 
            worldPos.y - chunkData.worldPosition.y,
            worldPos.z - chunkData.worldPosition.z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeshData CalculateMeshData(ChunkData chunkData)
    {
        MeshData meshData = new MeshData();

        IterateOverBlocks(chunkData, (x, y, z) =>
        {
            chunkData.blocks[x, y, z].AddFaceDataToMeshData(chunkData, x, y, z, ref meshData);
        });


        return meshData;
    }
    
    #endregion
    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            int chunkHorizontalSize = ChunkData.ChunkHorizontalSize;
            int chunkVerticalSize = ChunkData.ChunkVerticalSize;
            if (Selection.activeObject == gameObject)
            {
                Gizmos.color = new Color(0, 1, 0, 0.4f);
            }
            else
            {
                Gizmos.color = new Color(1, 0, 1, 0.4f);
                Gizmos.DrawCube(transform.position + new Vector3(chunkHorizontalSize / 2f - 0.5f, chunkVerticalSize / 2f - 0.5f, chunkHorizontalSize / 2f - 0.5f),
                    new Vector3(1f, 1f, 1f));
                return;
            }

            Gizmos.DrawCube(transform.position + new Vector3(chunkHorizontalSize / 2f - 0.5f, chunkVerticalSize / 2f - 0.5f, chunkHorizontalSize / 2f - 0.5f),
                new Vector3(chunkHorizontalSize + 0.001f, chunkVerticalSize + 0.001f, chunkHorizontalSize + 0.001f));
        }
    }
#endif
}
