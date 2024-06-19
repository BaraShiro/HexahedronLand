using System;
using System.Collections.Concurrent;
using System.Linq;
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
        private static readonly ConcurrentQueue<ChunkData> chunkDataObjectPool = new ConcurrentQueue<ChunkData>();

        public const int ChunkHorizontalSize = 16;
        public const int ChunkVerticalSize = 64;
        
        public readonly Block[][][] blocks = new Block[ChunkHorizontalSize][][];

        public World world;
        public Vector3Int worldPosition;

        private ChunkData(World world, Vector3Int worldPosition)
        {
            this.world = world;
            this.worldPosition = worldPosition;
            
            for (int x = 0; x < ChunkHorizontalSize; x++)
            {
                blocks[x] = new Block[ChunkHorizontalSize][];
                for (int y = 0; y < ChunkHorizontalSize; y++)
                {
                    blocks[x][y] = new Block[ChunkVerticalSize];
                }
            }
        }

        #region Chunk Data Object Pool

        public static ChunkData AcquireChunkData(World world, Vector3Int position)
        {
            ChunkData data;
            if (chunkDataObjectPool.IsEmpty)
            {
                data = new ChunkData(world, position);
            }
            else
            {
                if (chunkDataObjectPool.TryDequeue(out data))
                {
                    data.world = world;
                    data.worldPosition = position;
                    // Debug.Log($"Pulled chunk data {position} from pool");
                }
                else
                {
                    data =  new ChunkData(world, position);
                }
            }

            return data;
        }

        public static void ReleaseChunkData(ChunkData data)
        {
            if (chunkDataObjectPool.Contains(data))
            {
                Debug.LogError($"Data {data.worldPosition} already in pool");
                return;
            }

            for (int x = 0; x < ChunkHorizontalSize; x++)
            {
                for (int y = 0; y < ChunkHorizontalSize; y++)
                {
                    for (int z = 0; z < ChunkVerticalSize; z++)
                    {
                        data.blocks[x][y][z] = null;
                    }
                }
            }

            data.world = null;
            data.worldPosition = Vector3Int.zero;
            chunkDataObjectPool.Enqueue(data);
            // Debug.Log($"Returning chunk data {data.worldPosition} to pool");

        }

        #endregion
    }

    private static readonly ConcurrentQueue<Chunk> chunkObjectPool = new ConcurrentQueue<Chunk>();
    
    public const float TileSize = 0.125f; // TODO: Make changeable in inspector somehow
    public const float Offset = 0.001f; // Compensate for floating point rounding errors

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
        // Only save active chunks, not inactive chunks as they are pooled
        if (gameObject && gameObject.activeSelf)
        {
            Serialization.SaveChunk(Data);
        }
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

    private void ClearMeshes()
    {
        meshFilter.mesh.Clear();
        meshCollider.sharedMesh = null;
    }

    private void ClearFoliage()
    {
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }
    }

    #region Chunk Object Pool

    public static Chunk AcquireChunk(ChunkData chunkData, Chunk prefab, Vector3Int position, Quaternion rotation, Transform parent)
    {
        Chunk newChunk;

        if (chunkObjectPool.IsEmpty)
        {
            newChunk = Instantiate(prefab, position, rotation, parent);
        }
        else
        {
            if (chunkObjectPool.TryDequeue(out newChunk))
            {
                newChunk.transform.position = position;
                newChunk.transform.rotation = rotation;
                newChunk.transform.parent = parent;
                newChunk.gameObject.SetActive(true);
                // Debug.Log($"Pulled chunk {position} from pool");
            }
            else
            {
                newChunk = Instantiate(prefab, position, rotation, parent);
            }
        }

        newChunk.name = $"Chunk {position.x}, {position.y}, {position.z}";
        newChunk.Data = chunkData;

        return newChunk;
    }

    public static void ReleaseChunk(Chunk chunk)
    {
        // Debug.Log($"Returning chunk {chunk.Data.worldPosition} to pool");
        ChunkData.ReleaseChunkData(chunk.Data);
        chunk.Data = null;
        chunk.ClearMeshes();
        chunk.ClearFoliage();
        chunk.gameObject.name = "Chunk (Pooled)";
        chunk.gameObject.SetActive(false);
        chunkObjectPool.Enqueue(chunk);
    }

    #endregion
    
    #region Static functions
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IterateOverBlockPositions(ChunkData data, Action<int, int, int> action)
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
    private static bool WithinChunk(int x, int y, int z)
    {
        return InHorizontalRange(x) && InVerticalRange(y) && InHorizontalRange(z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InHorizontalRange(int index)
    {
        return index is >= 0 and < ChunkData.ChunkHorizontalSize;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InVerticalRange(int index)
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
        if(WithinChunk(localX, localY, localZ))
        {
            return chunkData.blocks[localX][localZ][localY];
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
            chunkData.blocks[localX][localZ][localY] = block;
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

        IterateOverBlockPositions(chunkData, (x, y, z) =>
        {
            chunkData.blocks[x][z][y].AddFaceDataToMeshData(chunkData, x, y, z, ref meshData);
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
