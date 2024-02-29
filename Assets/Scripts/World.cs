using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Chunk;
using static Chunk.ChunkData;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class World : SingletonMonoBehaviour<World>
{
    /// <summary>
    /// A class for keeping  
    /// </summary>
    private class ParallelizationUtils
    {
        public readonly CancellationTokenSource taskCancellationTokenSource;
        public readonly CancellationToken taskCancellationToken;
        public readonly ParallelOptions parallelOptions;

        public ParallelizationUtils()
        {
            taskCancellationTokenSource = new CancellationTokenSource();
            taskCancellationToken = taskCancellationTokenSource.Token;
            parallelOptions = new ParallelOptions
            {
                CancellationToken = taskCancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
        }
    }
    
    private class WorldData
    {
        public readonly ConcurrentDictionary<Vector3Int, ChunkData> chunkDataDictionary;
        public readonly ConcurrentDictionary<Vector3Int, Chunk> chunkDictionary;

        public WorldData()
        {
          chunkDataDictionary = new ConcurrentDictionary<Vector3Int, ChunkData>();
          chunkDictionary = new ConcurrentDictionary<Vector3Int, Chunk>();
        }
    }

    /// <summary>
    /// A prioritized position in world space.
    /// </summary>
    private readonly struct PrioritizedPosition : IEquatable<PrioritizedPosition>, IComparable, IComparable<PrioritizedPosition>
    {
        public readonly Vector3Int position;
        public readonly float priority;

        public PrioritizedPosition(Vector3Int position, float priority)
        {
            this.position = position;
            this.priority = priority;
        }

        public bool Equals(PrioritizedPosition other)
        {
            return position.Equals(other.position);
        }

        public int CompareTo(PrioritizedPosition other)
        {
            return priority.CompareTo(other.priority);
        }

        public int CompareTo(object obj)
        {
            return obj switch
            {
                null => 1,
                PrioritizedPosition otherPositionPriority => priority.CompareTo(otherPositionPriority.priority),
                _ => throw new ArgumentException("Object is not a PositionPriority")
            };
        }
    }
    
    /// <summary>
    /// The class in charge of keeping and maintaining the data needed for generating chunks. 
    /// </summary>
    private class WorldGenerationData
    {
        public readonly List<PrioritizedPosition> prioritizedChunkPositions;
        public readonly List<Vector3Int> chunkPositionsToCreate; // TODO: Don't keep duplicate data, the positions is same as in prio
        public readonly List<Vector3Int> chunkDataPositionsToCreate;
        public readonly List<Vector3Int> chunkPositionsToRemove;
        public readonly List<Vector3Int> chunkDataPositionsToRemove;

        public WorldGenerationData(WorldData worldData, Vector3Int generateAround, int radius)
        {
            GetChunkPositionsAroundPoint(worldData, generateAround, radius, out chunkPositionsToCreate, out prioritizedChunkPositions);
            GetChunkDataPositionsAroundPoint(worldData, generateAround, radius + 1, out chunkDataPositionsToCreate);
            GetChunksToRemove(worldData, generateAround, radius * 2, out chunkPositionsToRemove, out chunkDataPositionsToRemove);
        }

        /// <summary>
        /// Calculates the number of positions within a sphere. 
        /// </summary>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns>A number that is slightly larger than the number that fits in the sphere.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateCapacity(int radius)
        {
            // Add one to compensate for rounding
            int capacity1D = (radius * 2) + 1;
            int capacity3D = (int)(capacity1D * capacity1D * capacity1D * 0.55f); // TODO: Take ChunkVerticalSize into account
            return capacity3D;
        }

        private static void GetChunkPositionsAroundPoint(WorldData worldData, Vector3Int center, int radius, 
            out List<Vector3Int> positions, out List<PrioritizedPosition> priorities)
        {
            
            int capacity3D = CalculateCapacity(radius);
            positions = new List<Vector3Int>(capacity3D);
            priorities = new List<PrioritizedPosition>(capacity3D);

            for (int x = -radius; x <= radius; x++)
            {   
                // Generate less vertically as it is less visible
                for (int y = -1; y <= 1; y++) // TODO: Calculate correct value instead of hard coding
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        Vector3Int position = new Vector3Int(
                            center.x + (x * ChunkHorizontalSize), 
                            center.y + (y * ChunkVerticalSize),
                            center.z + (z * ChunkHorizontalSize));
                        float priority = Vector3Int.Distance(center, position);
                        if (!worldData.chunkDictionary.ContainsKey(position) && priority <= radius * ChunkHorizontalSize)
                        {
                            priorities.Add(new PrioritizedPosition(position, priority));
                            positions.Add(position);
                        }
                    }
                }
            }
            
            priorities.Sort();
        }
        
        private static void GetChunkDataPositionsAroundPoint(WorldData worldData, Vector3Int center, int radius, 
            out List<Vector3Int> dataPositions)
        {

            int capacity3D = CalculateCapacity(radius);
            dataPositions = new List<Vector3Int>(capacity3D);

            for (int x = -radius; x <= radius; x++)
            {   
                // Generate less vertically as it is less visible
                // Add one to radius to compensate for integer division
                for (int y = -2; y <= 2; y++) // TODO: Calculate correct value instead of hard coding
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        Vector3Int position = new Vector3Int(
                            center.x + (x * ChunkHorizontalSize), 
                            center.y + (y * ChunkVerticalSize),
                            center.z + (z * ChunkHorizontalSize));
                        if (!worldData.chunkDataDictionary.ContainsKey(position) && Vector3Int.Distance(center, position) <= radius * ChunkHorizontalSize)
                        {
                            dataPositions.Add(position);
                        }
                    }
                }
            }
        }

        private static void GetChunksToRemove(WorldData worldData, Vector3Int center, int radius,
            out List<Vector3Int> chunkPositions, out List<Vector3Int> chunkDataPositions)

        {

            int capacity = CalculateCapacity(radius);
            chunkPositions = new List<Vector3Int>(capacity);
            chunkDataPositions = new List<Vector3Int>(capacity);
            radius *= ChunkHorizontalSize;

            foreach (Vector3Int position in worldData.chunkDictionary.Keys)
            {
                if (Vector3Int.Distance(center, position) > radius)
                {
                    chunkPositions.Add(position);
                }
            }

            foreach (Vector3Int position in worldData.chunkDataDictionary.Keys)
            {
                if (Vector3Int.Distance(center, position) > radius + ChunkHorizontalSize)
                {
                    chunkDataPositions.Add(position);
                }
            }
        }
    }
    
    public class WorldGenerationStepEventArgs : EventArgs
    {
        public WorldGenerationStepEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
    
    public string worldName = "DefaultWorld";
    public string worldSeed = "Default";
    public string persistentDataPath;
    public LandscapeGenerator landscapeGenerator;
    public Transform chunkParent;

    public static Vector3Int WorldOffset { get; private set; } = Vector3Int.zero;

    [SerializeField, Range(1, 50)] private int drawRange = 8; // TODO: Make world setting
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private PlayerController playerPrefab;
    private PlayerController player;
    private Vector3 lastUpdatedPosition;

    private Vector3Int initialPoint = Vector3Int.zero; // TODO: load initial world spawn point from save file
    
    public static event EventHandler OnWorldGenerationStart;
    public static event EventHandler OnWorldGenerationFinish;
    public static event EventHandler<WorldGenerationStepEventArgs> OnWorldGenerationStep;

    private readonly WorldData worldData = new WorldData();
    private readonly Dictionary<string, SurfaceBiomeGenerator> biomeGenerators = new Dictionary<string, SurfaceBiomeGenerator>();
    private readonly ParallelizationUtils parallelizationUtils = new ParallelizationUtils();

    private const int SeedRandomRange = 10000;
    
   protected override void OnCreateInstance()
    {
        Random.InitState(worldSeed.GetHashCode());
        WorldOffset = new Vector3Int(
            Random.Range(-SeedRandomRange, SeedRandomRange), 
            Random.Range(-SeedRandomRange, SeedRandomRange), 
            Random.Range(-SeedRandomRange, SeedRandomRange)
        );
        persistentDataPath = Application.persistentDataPath;
    }

    protected override void OnFailedCreateInstance()
    {
        parallelizationUtils.taskCancellationTokenSource.Cancel();
    }

    protected override void OnDestroyInstance()
    {
        parallelizationUtils.taskCancellationTokenSource.Cancel();
        WorldOffset = Vector3Int.zero;
        OnWorldGenerationStart -= WorldGenerationLogger.WorldGenerationStart;
        OnWorldGenerationStep -= WorldGenerationLogger.AddStepToLog;
    }

    private void Start()
    {
        foreach (SurfaceBiomeGenerator generator in GetComponentsInChildren<SurfaceBiomeGenerator>())
        {
            if (!string.IsNullOrEmpty(generator.biomeName)) // TODO: make extension
            {
                biomeGenerators.TryAdd(generator.biomeName, generator);
            }
        }

        if (!chunkParent)
        {
            chunkParent = transform;
        }

        OnWorldGenerationStart += WorldGenerationLogger.WorldGenerationStart;
        OnWorldGenerationStep += WorldGenerationLogger.AddStepToLog;
        OnWorldGenerationFinish += SpawnPlayer;

        GenerateWorld();
    }
    
    private void OnApplicationQuit()
    {
        parallelizationUtils.taskCancellationTokenSource.Cancel();
    }

    private void FixedUpdate()
    {
        if (player) // TODO: use event
        {
            Vector3 position = player.GetPlayerPosition();
            if (Vector3.Distance(lastUpdatedPosition, position) > ChunkHorizontalSize)
            {
                Vector3Int playerPos = new Vector3Int(
                    Mathf.FloorToInt(position.x / ChunkHorizontalSize) * ChunkHorizontalSize,
                    Mathf.FloorToInt(position.y / ChunkHorizontalSize) * ChunkHorizontalSize,
                    Mathf.FloorToInt(position.z / ChunkHorizontalSize) * ChunkHorizontalSize
                );

                UpdateWorld(playerPos);
                lastUpdatedPosition = position;
            }
        }
    }

    private void SpawnPlayer(object sender, EventArgs e)
    {
        // TODO: find empty place as close to initial point as possible
        if (Physics.Raycast(transform.position + new Vector3(0f, 150f, 0f), Vector3.down, out RaycastHit hit, Mathf.Infinity))
        {
            player = Instantiate(playerPrefab, initialPoint + new Vector3(0, hit.point.y + 0.1f, 0), Quaternion.identity);
        }
        else
        {
            player = Instantiate(playerPrefab, initialPoint, Quaternion.identity); // TODO: Handle this better
        }

        OnWorldGenerationFinish -= SpawnPlayer;
    }
    
    private void WorldGenerationStepHandler(string message)
    {
        OnWorldGenerationStep?.Invoke(this, new WorldGenerationStepEventArgs(message));
    }

    public async void UpdateWorld(Vector3Int generateAround)
    {
        OnWorldGenerationStart?.Invoke(this, EventArgs.Empty);
        WorldGenerationStepHandler("Loading more chunks!");
        await GenerateWorld(generateAround, drawRange);
    }

    public async void UpdateWorld(Vector3Int generateAround, int radius)
    {
        OnWorldGenerationStart?.Invoke(this, EventArgs.Empty);
        WorldGenerationStepHandler("Loading more chunks!");
        await GenerateWorld(generateAround, radius);
    }

    private async void GenerateWorld()
    {
        OnWorldGenerationStart?.Invoke(this, EventArgs.Empty);
        await GenerateWorld(initialPoint, drawRange);
    }

    private async Task GenerateWorld(Vector3Int generateAround, int radius)
    {
        Stopwatch stopwatch = new Stopwatch();
        
        try
        {
            WorldGenerationStepHandler($"Generating biome points around point {generateAround}...");
            stopwatch.Restart();
            await GenerateBiomePointsAsync(generateAround, parallelizationUtils.taskCancellationToken);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Generated biome points in {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            WorldGenerationStepHandler("GenerateBiomePoints task canceled!");
            return;
        }

        WorldGenerationData worldGenerationData;
        
        try
        {
            WorldGenerationStepHandler($"Calculating world generation data...");
            stopwatch.Restart();
            worldGenerationData = await GenerateWorldGenerationDataAsync(generateAround, radius, parallelizationUtils.taskCancellationToken);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Calculated {worldGenerationData.chunkDataPositionsToCreate.Count} data positions and {worldGenerationData.chunkPositionsToCreate.Count} chunk positions in {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            WorldGenerationStepHandler("GenerateWorldGenerationData task canceled!");
            return;
        }

        if (worldGenerationData == null)
        {
            WorldGenerationStepHandler("Failed to generate WorldGenerationData, aborting world generation!");
            return;
        }
        
        foreach (Vector3Int position in worldGenerationData.chunkPositionsToRemove)
        {
            DestroyChunk(position);
        }
        
        foreach (Vector3Int position in worldGenerationData.chunkDataPositionsToRemove)
        {
            worldData.chunkDataDictionary.TryRemove(position, out _);
        }
        
        ConcurrentDictionary<Vector3Int, ChunkData> chunkDataDictionary;
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDictionary;

        try
        {
            WorldGenerationStepHandler($"Generating chunk data...");
            WorldGenerationLogger.ClearChunkGenerationDetails();
            stopwatch.Restart();
            chunkDataDictionary = await GenerateChunkDataParallelAsync(worldGenerationData.chunkDataPositionsToCreate);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Generated {worldGenerationData.chunkDataPositionsToCreate.Count} chunks in {stopwatch.ElapsedMilliseconds} ms\n" +
                                       $"Time spent in SelectBiome: {WorldGenerationLogger.GetSelectBiomeDetails}\n" +
                                       $"Time spent in HandleLayer: {WorldGenerationLogger.GetHandleLayerDetails}");
        }
        catch (OperationCanceledException)
        {
            WorldGenerationStepHandler("GenerateChunkData task canceled!");
            return;
        }


        WorldGenerationStepHandler($"Instantiating chunks...");
        stopwatch.Restart();
        foreach (var (chunkPosition, chunkData) in chunkDataDictionary)
        {
            worldData.chunkDataDictionary.TryAdd(chunkPosition, chunkData);
        }

        foreach (Vector3Int position in worldGenerationData.chunkPositionsToCreate)
        {
            if(worldData.chunkDataDictionary.TryGetValue(position, out ChunkData data))
            {
                worldData.chunkDictionary.TryAdd(position, CreateChunk(position, data));
            }
        }
        stopwatch.Stop();
        WorldGenerationStepHandler($"Instantiated {chunkDataDictionary.Count} chunks in {stopwatch.ElapsedMilliseconds} ms");
        
        try
        {
            WorldGenerationStepHandler($"Generating meshes...");
            stopwatch.Restart();
            meshDataDictionary = await GenerateMeshDataParallelAsync(worldGenerationData.chunkPositionsToCreate);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Generated {worldGenerationData.chunkPositionsToCreate.Count} meshes in {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            WorldGenerationStepHandler("GenerateMeshData task canceled!");
            return;
        }

        StartCoroutine(RenderChunksRoutine(meshDataDictionary, worldGenerationData.prioritizedChunkPositions));
    }

    private Task GenerateBiomePointsAsync(Vector3Int generateAround, CancellationToken token)
    {
        return Task.Run(() =>
        {
            landscapeGenerator.GenerateBiomePoints(generateAround, 2);
            token.ThrowIfCancellationRequested();
        }, token);
    }

    private Task<WorldGenerationData> GenerateWorldGenerationDataAsync(Vector3Int generateAround, int radius, CancellationToken token)
    {
        WorldGenerationData worldGenerationData;
        
        return Task.Run(() =>
        {
            worldGenerationData = new WorldGenerationData(worldData, generateAround, radius);
            
            token.ThrowIfCancellationRequested();

            return worldGenerationData;
        }, token);
        
    }

    private Task<ConcurrentDictionary<Vector3Int, ChunkData>> GenerateChunkDataParallelAsync(List<Vector3Int> chunkDataPositions)
    {
        ConcurrentDictionary<Vector3Int, ChunkData> chunkDataDictionary = new ConcurrentDictionary<Vector3Int, ChunkData>();

        return Task.Run(() =>
            {
                foreach (Vector3Int position in chunkDataPositions)
                {
                    parallelizationUtils.taskCancellationToken.ThrowIfCancellationRequested();
                    
                    ChunkData chunkData = new ChunkData(this, position);
                    chunkDataDictionary.TryAdd(position, chunkData);
                    landscapeGenerator.GenerateChunkDataParallel(chunkData, parallelizationUtils.parallelOptions);
                    Serialization.LoadChunk(ref chunkData);
                }
                
                parallelizationUtils.taskCancellationToken.ThrowIfCancellationRequested();
                
                return chunkDataDictionary;
            }, parallelizationUtils.taskCancellationToken);
    }

    private Task<ConcurrentDictionary<Vector3Int, MeshData>> GenerateMeshDataParallelAsync(List<Vector3Int> chunkPositionsToCreate)
    {
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDictionary = new ConcurrentDictionary<Vector3Int, MeshData>();

        return Task.Run(() =>
            {
                Parallel.ForEach(chunkPositionsToCreate, parallelizationUtils.parallelOptions, (position) =>
                {
                    if(worldData.chunkDataDictionary.TryGetValue(position, out ChunkData chunkData))
                    {
                        MeshData meshData = CalculateMeshData(chunkData);
                        meshDataDictionary.TryAdd(chunkData.worldPosition, meshData);
                    }
                });

                parallelizationUtils.taskCancellationToken.ThrowIfCancellationRequested();
            
                return meshDataDictionary;
            }, parallelizationUtils.taskCancellationToken);
    }

    private Chunk CreateChunk(Vector3Int chunkPosition, ChunkData chunkData)
    {
        Chunk newChunk = Instantiate(chunkPrefab, new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z), Quaternion.identity, chunkParent);
        newChunk.name = $"Chunk {chunkPosition.x}, {chunkPosition.y}, {chunkPosition.z}";
        newChunk.InitializeChunk(chunkData);
        return newChunk;
    }

    private void DestroyChunk(Vector3Int chunkPosition)
    {
        if (worldData.chunkDictionary.TryGetValue(chunkPosition, out Chunk chunk))
        {
            Serialization.SaveChunk(chunk.Data);
            worldData.chunkDictionary.TryRemove(chunkPosition, out _);
            Destroy(chunk.gameObject);
        }
    }

    private Chunk GetChunk(Vector3Int blockPos)
    {
        const float multipleH = ChunkHorizontalSize;
        const float multipleV = ChunkHorizontalSize;
        blockPos.x = Mathf.FloorToInt(blockPos.x / multipleH ) * ChunkHorizontalSize;
        blockPos.y = Mathf.FloorToInt(blockPos.y / multipleV ) * ChunkVerticalSize;
        blockPos.z = Mathf.FloorToInt(blockPos.z / multipleH ) * ChunkHorizontalSize;

        worldData.chunkDictionary.TryGetValue(blockPos, out Chunk chunk);
  
        return chunk;
    }

    private ChunkData GetChunkData(Vector3Int blockPos)
    {
        const float multipleH = ChunkHorizontalSize;
        const float multipleV = ChunkVerticalSize;
        blockPos.x = Mathf.FloorToInt(blockPos.x / multipleH ) * ChunkHorizontalSize;
        blockPos.y = Mathf.FloorToInt(blockPos.y / multipleV ) * ChunkVerticalSize;
        blockPos.z = Mathf.FloorToInt(blockPos.z / multipleH ) * ChunkHorizontalSize;

        worldData.chunkDataDictionary.TryGetValue(blockPos, out ChunkData chunkData);
  
        return chunkData;
    }

    public Block GetBlock(int worldCoordX, int worldCoordY, int worldCoordZ)
    {
        ChunkData containerChunkData = GetChunkData(new Vector3Int(worldCoordX, worldCoordY, worldCoordZ));

        if (containerChunkData != null)
        {
            Block block = Chunk.GetBlock(
                containerChunkData,
                worldCoordX - containerChunkData.worldPosition.x,
                worldCoordY -containerChunkData.worldPosition.y, 
                worldCoordZ - containerChunkData.worldPosition.z);
  
            return block;
        }
        else
        {
            // return BlockAir.Instance;
            // Debug.Log($"Container chunk data is null");
            return new Block(Block.VoidBlockName);
        }
    }

    public void SetBlock(int worldCoordX, int worldCoordY, int worldCoordZ, Block block)
    {
        Vector3Int worldPosition = new Vector3Int(worldCoordX, worldCoordY, worldCoordZ);
        
        Chunk chunk = GetChunk(worldPosition);
        if (chunk)
        {
            Vector3Int localPosition = worldPosition - chunk.Data.worldPosition;
            
            SetBlockLocal(chunk.Data, localPosition, block);
            chunk.UpdateChunk();

            if (localPosition.x is 0)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.left);
            }
            else if (localPosition.x is ChunkHorizontalSize - 1)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.right);
            }

            if (localPosition.y is 0)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.down);
            }
            else if (localPosition.y is ChunkVerticalSize - 1)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.up);
            }

            if (localPosition.z is 0)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.back);
            }
            else if (localPosition.z is ChunkHorizontalSize - 1)
            {
                UpdateNeighbouringChunk(worldPosition + Vector3Int.forward);
            }
        }
        
        void UpdateNeighbouringChunk(Vector3Int neighbouringWorldPosition)
        {
            Chunk neighbouringChunk = GetChunk(neighbouringWorldPosition);
            if (neighbouringChunk)
            {
                neighbouringChunk.UpdateChunk();
            }
        }
    }

    private IEnumerator RenderChunksRoutine(ConcurrentDictionary<Vector3Int, MeshData> meshDataDictionary, List<PrioritizedPosition> chunkPositionsToRender)
    {
        void PlaceVegetation(int x, int y, int z, Block block, Chunk chunk, Vegetation.VegetationData vegetationData)
        {
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, (x + 1) * (x + 1) * (z + 1) * (z + 1), 0f));
            Vector3 position = new Vector3(chunk.Data.worldPosition.x + x, chunk.Data.worldPosition.y + y + 0.5f, chunk.Data.worldPosition.z + z);
            // TODO: Make vegetation struct that keeps info on vegetation (offset in deco array, axis to rotate on, offset in pos)
            if(biomeGenerators.TryGetValue(vegetationData.biome, out SurfaceBiomeGenerator biomeGenerator) && biomeGenerator.vegetation)
            {
                Vegetation vegetationPrefab = biomeGenerator.vegetation.GetVegetation(vegetationData.type, x, z);
                if(vegetationPrefab)
                {
                    Vegetation deco = Instantiate(vegetationPrefab, position, rotation, chunk.transform);
                    deco.ParentBlock = block;
                }
            }
        }
        
        Stopwatch stopwatch = new Stopwatch();
        
        WorldGenerationStepHandler($"Rendering chunks...");
        stopwatch.Restart();
        yield return null;

        int iterations = 0;
        int iterationsPerFrame = 8;

        foreach (PrioritizedPosition positionPriority in chunkPositionsToRender)
        {
            if (meshDataDictionary.TryGetValue(positionPriority.position, out MeshData meshData))
            {
                if (worldData.chunkDictionary.TryGetValue(positionPriority.position, out Chunk chunk))
                {
                    chunk.UpdateChunk(meshData);
                    IterateOverBlocks(chunk.Data, (x, y, z) =>
                    {
                        Block block = chunk.Data.blocks[x, y, z];
                        if (block.vegetation.HasValue)
                        {
                            PlaceVegetation(x, y, z, block, chunk, block.vegetation.Value);
                        }
                    });
                }
            }
            
            iterations++;
            if(iterations >= iterationsPerFrame)
            {
                iterations = 0;
                yield return null;
            }
        }
        stopwatch.Stop();
        WorldGenerationStepHandler($"Rendered {chunkPositionsToRender.Count} chunks in {stopwatch.ElapsedMilliseconds} ms");

        yield return null;
        
        OnWorldGenerationFinish?.Invoke(this, EventArgs.Empty);
    }
}
