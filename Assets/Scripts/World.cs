using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using static Chunk;
using static Chunk.ChunkData;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class World : SingletonMonoBehaviour<World>
{
    /// <summary>
    /// A class for keeping ParallelOptions and CancellationTokens.
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
        public readonly ConcurrentDictionary<Vector3Int, ChunkData> chunkDataDictionary = new ConcurrentDictionary<Vector3Int, ChunkData>();
        public readonly ConcurrentDictionary<Vector3Int, Chunk> chunkDictionary = new ConcurrentDictionary<Vector3Int, Chunk>();
    }

    /// <summary>
    /// A prioritized position in world space.
    /// </summary>
    private readonly struct PrioritizedPosition : IEquatable<PrioritizedPosition>, IComparable, IComparable<PrioritizedPosition>
    {
        public readonly Vector3Int position;
        private readonly float priority;

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
                _ => throw new ArgumentException("Object is not a PrioritizedPosition")
            };
        }
    }
    
    /// <summary>
    /// The class in charge of keeping and maintaining the data needed for generating chunks. 
    /// </summary>
    private class WorldGenerationData
    {
        public readonly List<PrioritizedPosition> prioritizedChunkPositions;
        public readonly List<Vector3Int> chunkDataPositionsToCreate;
        public readonly List<Vector3Int> chunkPositionsToRemove;
        public readonly List<Vector3Int> chunkDataPositionsToRemove;

        private int radius;
        private int verticalRadius;

        public WorldGenerationData(WorldData worldData, Vector3Int generateAround, int radius)
        {
            this.radius = radius < 1 ? 1 : radius;
            this.verticalRadius = CalculateVerticalRadius(radius);
            
            GetChunkPositionsAroundPoint(in worldData, generateAround, out prioritizedChunkPositions);
            GetChunkDataPositionsAroundPoint(in worldData, generateAround, out chunkDataPositionsToCreate);
            GetChunksToRemove(in worldData, generateAround, out chunkPositionsToRemove, out chunkDataPositionsToRemove);
        }

        /// <summary>
        /// Calculates the number of positions within a sphere. 
        /// </summary>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns>A number that is slightly larger than the number that fits in the sphere.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateCapacity(int radius)
        {
            const float cubeToSphere = 0.55f;
            const float verticalScale = (float) ChunkHorizontalSize / ChunkVerticalSize;
            int capacity1D = (radius * 2) + 1; // Add one to compensate for rounding
            float capacity1DVertical = capacity1D * verticalScale;
            int capacity3D = (int)(capacity1D * capacity1D * capacity1DVertical * cubeToSphere);
            return capacity3D;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateVerticalRadius(int radius)
        {
            const float verticalScale = (float) ChunkHorizontalSize / ChunkVerticalSize;
            float verticalRadius = radius * verticalScale;
            verticalRadius *= 0.5f; // Generate less vertically as it is less visible
            if (verticalRadius < 1) verticalRadius = 1f;
            return Mathf.RoundToInt(verticalRadius);
        }

        private void GetChunkPositionsAroundPoint(in WorldData worldData, Vector3Int center, 
            out List<PrioritizedPosition> prioritizedPositions)
        {
            int capacity3D = CalculateCapacity(radius);
            prioritizedPositions = new List<PrioritizedPosition>(capacity3D);

            for (int x = -radius; x <= radius; x++)
            {   
                for (int y = -verticalRadius; y <= verticalRadius; y++)
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
                            prioritizedPositions.Add(new PrioritizedPosition(position, priority));
                        }
                    }
                }
            }
            
            prioritizedPositions.Sort();
        }
        
        private void GetChunkDataPositionsAroundPoint(in WorldData worldData, Vector3Int center, 
            out List<Vector3Int> dataPositions)
        {
            // Add one to generate more data positions than chunk positions
            radius += 1;
            verticalRadius += 1;

            int capacity3D = CalculateCapacity(radius);
            dataPositions = new List<Vector3Int>(capacity3D);

            for (int x = -radius; x <= radius; x++)
            {   
                for (int y = -verticalRadius; y <= verticalRadius; y++)
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

        private void GetChunksToRemove(in WorldData worldData, Vector3Int center,
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
    private readonly float updateDelay = 2f;
    private float timeSinceLastUpdate = 0f;
    private bool doneUpdating = false;

    [SerializeField] private Vector3Int initialPoint = Vector3Int.zero; // TODO: load initial world spawn point from save file
    
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

        // get_persistentDataPath can only be called from the main thread, so we cache it here.
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
        OnWorldGenerationFinish += MarkUpdateFinished;

        GenerateWorld();
    }
    
    private void OnApplicationQuit()
    {
        parallelizationUtils.taskCancellationTokenSource.Cancel();
    }

    private void FixedUpdate()
    {

        if (doneUpdating && timeSinceLastUpdate >= updateDelay)
        {
            if (player)
            {
                timeSinceLastUpdate = 0f;
                Vector3 position = player.GetPlayerPosition();
                UpdateWorld(position);
            }
        }
        else
        {
            timeSinceLastUpdate += Time.fixedDeltaTime;
        }
    }

    private void MarkUpdateFinished(object sender, EventArgs e)
    {
        doneUpdating = true;
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

    private void UpdateWorld(Vector3 generateAround)
    {
        // Update world asynchronously
        UpdateWorld(generateAround, drawRange);
    }

    private async void UpdateWorld(Vector3 generateAround, int radius)
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

    private async Task GenerateWorld(Vector3 centerPosition, int radius)
    {
        doneUpdating = false;
        Vector3Int centerChunkPosition = new Vector3Int(
            Mathf.FloorToInt(centerPosition.x / ChunkHorizontalSize) * ChunkHorizontalSize,
            Mathf.FloorToInt(centerPosition.y / ChunkVerticalSize) * ChunkVerticalSize,
            Mathf.FloorToInt(centerPosition.z / ChunkHorizontalSize) * ChunkHorizontalSize
        );

        Stopwatch stopwatch = new Stopwatch();
        
        try
        {
            WorldGenerationStepHandler($"Generating biome points around point {centerChunkPosition}...");
            stopwatch.Restart();
            await GenerateBiomePointsAsync(centerChunkPosition, parallelizationUtils.taskCancellationToken);
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
            worldGenerationData = await GenerateWorldGenerationDataAsync(centerChunkPosition, radius, parallelizationUtils.taskCancellationToken);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Calculated {worldGenerationData.chunkDataPositionsToCreate.Count} data positions and {worldGenerationData.prioritizedChunkPositions.Count} chunk positions in {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            WorldGenerationStepHandler("GenerateWorldGenerationData task canceled!");
            return;
        }

        foreach (Vector3Int position in worldGenerationData.chunkPositionsToRemove)
        {
            if (worldGenerationData.chunkDataPositionsToRemove.Contains(position))
            {
                Debug.LogWarning($"Found chunk pos {position} in chunk data");
                worldGenerationData.chunkDataPositionsToRemove.Remove(position);
            }

            if (worldData.chunkDictionary.TryRemove(position, out Chunk chunk))
            {
                worldData.chunkDataDictionary.TryRemove(position, out ChunkData data);
                Serialization.SaveChunk(chunk.Data);
                Chunk.ReleaseChunk(chunk);
            }
        }

        foreach (Vector3Int position in worldGenerationData.chunkDataPositionsToRemove)
        {
            if (worldData.chunkDataDictionary.TryRemove(position, out ChunkData data))
            {
                ChunkData.ReleaseChunkData(data);
            }
        }

        ConcurrentDictionary<Vector3Int, ChunkData> chunkDataDictionary;
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDictionary;

        try
        {
            WorldGenerationStepHandler($"Generating chunk data...");
            WorldGenerationLogger.ClearChunkGenerationDetails();
            stopwatch.Restart();
            chunkDataDictionary = await GenerateChunkDataParallelAsync(worldGenerationData.chunkDataPositionsToCreate); // TODO: Put chunk data directly in worldData.chunkDataDictionary
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
        foreach ((Vector3Int chunkPosition, ChunkData chunkData) in chunkDataDictionary)
        {
            worldData.chunkDataDictionary.TryAdd(chunkPosition, chunkData);
        }

        foreach (PrioritizedPosition prioritizedPosition in worldGenerationData.prioritizedChunkPositions)
        {
            if(worldData.chunkDataDictionary.TryGetValue(prioritizedPosition.position, out ChunkData data))
            {
                worldData.chunkDictionary.TryAdd(prioritizedPosition.position, CreateChunk(prioritizedPosition.position, data));
            }
        }
        stopwatch.Stop();
        WorldGenerationStepHandler($"Instantiated {chunkDataDictionary.Count} chunks in {stopwatch.ElapsedMilliseconds} ms");
        
        try
        {
            WorldGenerationStepHandler($"Generating meshes...");
            stopwatch.Restart();
            meshDataDictionary = await GenerateMeshDataParallelAsync(worldGenerationData.prioritizedChunkPositions);
            stopwatch.Stop();
            WorldGenerationStepHandler($"Generated {worldGenerationData.prioritizedChunkPositions.Count} meshes in {stopwatch.ElapsedMilliseconds} ms");
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
            landscapeGenerator.UpdateBiomeCenterPoints(generateAround);
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
                    
                    ChunkData chunkData = ChunkData.AcquireChunkData(this, position);
                    chunkDataDictionary.TryAdd(position, chunkData);
                    landscapeGenerator.GenerateChunkDataParallel(chunkData, parallelizationUtils.parallelOptions);
                    Serialization.LoadChunk(ref chunkData);
                }
                
                parallelizationUtils.taskCancellationToken.ThrowIfCancellationRequested();
                
                return chunkDataDictionary;
            }, parallelizationUtils.taskCancellationToken);
    }

    private Task<ConcurrentDictionary<Vector3Int, MeshData>> GenerateMeshDataParallelAsync(List<PrioritizedPosition> prioritizedPositions)
    {
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDictionary = new ConcurrentDictionary<Vector3Int, MeshData>();

        return Task.Run(() =>
            {
                Parallel.ForEach(prioritizedPositions, parallelizationUtils.parallelOptions, (prioritizedPosition) =>
                {
                    parallelizationUtils.taskCancellationToken.ThrowIfCancellationRequested();
                    
                    if(worldData.chunkDataDictionary.TryGetValue(prioritizedPosition.position, out ChunkData chunkData))
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
        Chunk newChunk = Chunk.AcquireChunk(chunkData, chunkPrefab, chunkPosition, Quaternion.identity, chunkParent);
        return newChunk;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3Int BlockPositionToChunkPosition(Vector3Int blockPosition)
    {
        // FloorToInt of floating point division for correct negative positions
        blockPosition.x = Mathf.FloorToInt(blockPosition.x / (float) ChunkHorizontalSize) * ChunkHorizontalSize;
        blockPosition.y = Mathf.FloorToInt(blockPosition.y / (float) ChunkVerticalSize) * ChunkVerticalSize;
        blockPosition.z = Mathf.FloorToInt(blockPosition.z / (float) ChunkHorizontalSize) * ChunkHorizontalSize;
        
        return blockPosition;
    }

    private Chunk GetChunkFromBlockPosition(Vector3Int blockPosition)
    {
        Vector3Int chunkPosition = BlockPositionToChunkPosition(blockPosition);
        worldData.chunkDictionary.TryGetValue(chunkPosition, out Chunk chunk);
  
        return chunk;
    }

    private ChunkData GetChunkDataFromBlockPosition(Vector3Int blockPosition)
    {
        Vector3Int chunkPosition = BlockPositionToChunkPosition(blockPosition);
        worldData.chunkDataDictionary.TryGetValue(chunkPosition, out ChunkData chunkData);
  
        return chunkData;
    }

    public Block GetBlock(int worldPositionX, int worldPositionY, int worldPositionZ)
    {
        ChunkData containerChunkData = GetChunkDataFromBlockPosition(new Vector3Int(worldPositionX, worldPositionY, worldPositionZ));

        if (containerChunkData != null)
        {
            Block block = Chunk.GetBlock(
                containerChunkData,
                worldPositionX - containerChunkData.worldPosition.x,
                worldPositionY - containerChunkData.worldPosition.y, 
                worldPositionZ - containerChunkData.worldPosition.z);
  
            return block;
        }
        else
        {
            // return BlockAir.Instance;
            // Debug.Log($"Container chunk data is null");
            return new Block(Block.VoidBlockName);
        }
    }

    public void SetBlock(int worldPositionX, int worldPositionY, int worldPositionZ, Block block)
    {
        Vector3Int worldPosition = new Vector3Int(worldPositionX, worldPositionY, worldPositionZ);
        
        Chunk chunk = GetChunkFromBlockPosition(worldPosition);
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
            Chunk neighbouringChunk = GetChunkFromBlockPosition(neighbouringWorldPosition);
            if (neighbouringChunk)
            {
                neighbouringChunk.UpdateChunk();
            }
        }
    }

    #region Coroutines
    
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
                    IterateOverBlockPositions(chunk.Data, (x, y, z) =>
                    {
                        Block block = chunk.Data.blocks[x][z][y];
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
    
    #endregion
}
