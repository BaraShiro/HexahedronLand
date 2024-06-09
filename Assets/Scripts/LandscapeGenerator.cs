using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

public class LandscapeGenerator : MonoBehaviour
{
    private struct BiomeData
    {
        public readonly Vector2Int center;
        public readonly float distance;
        public readonly float temperature;
        public readonly float precipitation;

        public BiomeData(Vector2Int center, float distance, float temperature, float precipitation)
        {
            this.center = center;
            this.distance = distance;
            this.temperature = temperature;
            this.precipitation = precipitation;
        }
    }
    
    private struct BiomeCenters
    {
        public BiomeData northWestBiome;
        public BiomeData northEastBiome;
        public BiomeData southWestBiome;
        public BiomeData southEastBiome;

        public BiomeCenters(BiomeData northWestBiome, BiomeData northEastBiome, BiomeData southWestBiome, BiomeData southEastBiome)
        {
            this.northWestBiome = northWestBiome;
            this.northEastBiome = northEastBiome;
            this.southWestBiome = southWestBiome;
            this.southEastBiome = southEastBiome;
        }

        public BiomeData GetClosest(Vector2Int point)
        {
            (float distance, BiomeData data) nw = (Vector2Int.Distance(northWestBiome.center, point), northWestBiome);
            (float distance, BiomeData data) ne = (Vector2Int.Distance(northEastBiome.center, point), northEastBiome);
            (float distance, BiomeData data) se = (Vector2Int.Distance(southEastBiome.center, point), southEastBiome);
            (float distance, BiomeData data) sw = (Vector2Int.Distance(southWestBiome.center, point), southWestBiome);
            (float distance, BiomeData data) n = nw.distance < ne.distance ? nw : ne;
            (float distance, BiomeData data) s = se.distance < sw.distance ? se : sw;
            return n.distance < s.distance ? n.data : s.data;
        }
    }

    [SerializeField] private NoiseSettings biomeTemperatureNoiseSettings;
    [SerializeField] private NoiseSettings biomePrecipitationNoiseSettings;
    [SerializeField] private DomainWarping biomeDomainWarping;
    [SerializeReference] private BiomeClimateData biomeClimateData = new BiomeClimateData(1,1);
    public bool useDomainWarping = true;

    public const int BiomeSize = 8 * Chunk.ChunkData.ChunkHorizontalSize;

    private Vector2Int currentBiomePosition = Vector2Int.zero;
    private List<Vector2Int> biomeCenterPoints = new List<Vector2Int>();
    private ConcurrentDictionary<Vector2Int, BiomeData> biomes;
    private bool firstGeneration = true;

    private void SelectBiome(Vector2Int worldPosition, out SurfaceBiomeGenerator biomeGenerator, out int rockHeight, out int dirtHeight)
    {
        if(useDomainWarping)
        {
            Vector2Int domainOffset = biomeDomainWarping.GenerateDomainOffsetInt(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);
            worldPosition += new Vector2Int(domainOffset.x, domainOffset.y);
        }
        
        BiomeCenters biomeCenters =  CalculateBiomeData(worldPosition);
        
        float2x4 points = new float2x4
            (
                new float2(biomeCenters.northWestBiome.center.x, biomeCenters.northWestBiome.center.y),
                new float2(biomeCenters.northEastBiome.center.x, biomeCenters.northEastBiome.center.y),
                new float2(biomeCenters.southEastBiome.center.x, biomeCenters.southEastBiome.center.y),
                new float2(biomeCenters.southWestBiome.center.x, biomeCenters.southWestBiome.center.y)
            );

        float4x2 heights = new float4x2();
        
        SurfaceBiomeGenerator generator = SelectBiomeGenerator(in biomeCenters.northWestBiome);
        (heights.c0.x, heights.c1.x) = generator.GetSurfaceHeightNoise(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);

        generator = SelectBiomeGenerator(in biomeCenters.northEastBiome);
        (heights.c0.y, heights.c1.y) = generator.GetSurfaceHeightNoise(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);

        generator = SelectBiomeGenerator(in biomeCenters.southEastBiome);
        (heights.c0.z, heights.c1.z) = generator.GetSurfaceHeightNoise(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);

        generator = SelectBiomeGenerator(in biomeCenters.southWestBiome);
        (heights.c0.w, heights.c1.w) = generator.GetSurfaceHeightNoise(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);

        float2 point = new float2(worldPosition.x, worldPosition.y);
        MathHelpers.InverseDistanceCubedWeighting(in point, in points, in heights, out float2 weightedHeights);

        biomeGenerator = SelectBiomeGenerator(biomeCenters.GetClosest(new Vector2Int(worldPosition.x, worldPosition.y)));
        rockHeight = Mathf.RoundToInt(weightedHeights.x);
        dirtHeight = Mathf.RoundToInt(weightedHeights.y);
    }

    [ContextMenu("InverseTest")]
    public void InverseTest()
    {
        float2 point = new float2(1f, 1f);
        float2x4 points = new float2x4
        (
            new float2(0.5f, 0.9f), 
            new float2(1.5f, 1.5f), 
            new float2(1f, 0.5f), 
            new float2(0.5f, 1.4f) 
        );
        float4 values = new float4 { x = 1f, y = 3f, z = 5f, w = 7f};

        float result = MathHelpers.InverseDistanceWeighting(point, points , values);
        Debug.Log(result);

    }

    public void GenerateChunkDataParallel(Chunk.ChunkData chunkData, ParallelOptions parallelOptions)
    {
        //TODO: check if chunk is underground and use underground generator if it is

        for (int x = chunkData.worldPosition.x; x < chunkData.worldPosition.x + Chunk.ChunkData.ChunkHorizontalSize; x++)
        {
            int capturedX = x;
            Parallel.For(chunkData.worldPosition.z, chunkData.worldPosition.z + Chunk.ChunkData.ChunkHorizontalSize, parallelOptions, (z) =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                
                SelectBiome(new Vector2Int(capturedX, z), out SurfaceBiomeGenerator biomeGenerator, out int rockHeight, out int dirtHeight);
                
                stopwatch.Stop();
                long select = stopwatch.ElapsedTicks;

                stopwatch.Restart();
                
                chunkData = biomeGenerator.GenerateChunkColumn(chunkData, capturedX, z, rockHeight, dirtHeight);
                
                stopwatch.Stop();
                long handle = stopwatch.ElapsedTicks;
                
                WorldGenerationLogger.SelectBiomeTicks.Add(select);
                WorldGenerationLogger.HandleLayerTicks.Add(handle);

            });
        }
        
    }

    private BiomeCenters CalculateBiomeData(Vector2Int horizontalPosition)
    {
        Vector2Int northWestPoint = horizontalPosition;
        Vector2Int northEastPoint = horizontalPosition;
        Vector2Int southWestPoint = horizontalPosition;
        Vector2Int southEastPoint = horizontalPosition;

        float northWestShortestDistance = float.PositiveInfinity;
        float northEastShortestDistance = float.PositiveInfinity;
        float southWestShortestDistance = float.PositiveInfinity;
        float southEastShortestDistance = float.PositiveInfinity;

        foreach (Vector2Int point in biomeCenterPoints)
        {
            float distance = Vector2Int.Distance(horizontalPosition, point);

            // North
            if (point.y >= horizontalPosition.y)
            {
                // East
                if (point.x >= horizontalPosition.x)
                {
                    if (distance < northEastShortestDistance)
                    {
                        northEastShortestDistance = distance;
                        northEastPoint = point;
                    }
                }
                // West
                else
                {
                    if (distance < northWestShortestDistance)
                    {
                        northWestShortestDistance = distance;
                        northWestPoint = point;
                    }
                }
            }
            //South
            else
            {
                // East
                if (point.x >= horizontalPosition.x)
                {
                    if (distance < southEastShortestDistance)
                    {
                        southEastShortestDistance = distance;
                        southEastPoint = point;
                    }
                }
                // West
                else
                {
                    if (distance < southWestShortestDistance)
                    {
                        southWestShortestDistance = distance;
                        southWestPoint = point;
                    }
                }
            }
        }

        BiomeData northWestBiome = new BiomeData(
            northWestPoint,
            northWestShortestDistance,
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                northWestPoint.x + World.WorldOffset.x,
                northWestPoint.y + World.WorldOffset.z,
                in biomeTemperatureNoiseSettings.settingsData),
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                northWestPoint.x + World.WorldOffset.x,
                northWestPoint.y + World.WorldOffset.z,
                in biomePrecipitationNoiseSettings.settingsData)
            );

        BiomeData northEastBiome = new BiomeData(
            northEastPoint,
            northEastShortestDistance,
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                northEastPoint.x + World.WorldOffset.x,
                northEastPoint.y + World.WorldOffset.z,
                in biomeTemperatureNoiseSettings.settingsData),
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                northEastPoint.x + World.WorldOffset.x,
                northEastPoint.y + World.WorldOffset.z,
                in biomePrecipitationNoiseSettings.settingsData)
        );

        BiomeData southWestBiome = new BiomeData(
            southWestPoint,
            southWestShortestDistance,
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                southWestPoint.x + World.WorldOffset.x,
                southWestPoint.y + World.WorldOffset.z,
                in biomeTemperatureNoiseSettings.settingsData),
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                southWestPoint.x + World.WorldOffset.x,
                southWestPoint.y + World.WorldOffset.z,
                in biomePrecipitationNoiseSettings.settingsData)
        );

        BiomeData southEastBiome = new BiomeData(
            southEastPoint,
            southEastShortestDistance,
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                southEastPoint.x + World.WorldOffset.x,
                southEastPoint.y + World.WorldOffset.z,
                in biomeTemperatureNoiseSettings.settingsData),
            NoiseGenerator.OctaveSimplexNoiseBurstCompiled(
                southEastPoint.x + World.WorldOffset.x,
                southEastPoint.y + World.WorldOffset.z,
                in biomePrecipitationNoiseSettings.settingsData)
        );

        return new BiomeCenters(northWestBiome, northEastBiome, southWestBiome, southEastBiome);
    }

    private ref SurfaceBiomeGenerator SelectBiomeGenerator(in BiomeData biomeData)
    {
      return ref biomeClimateData.GetBiome(biomeData.precipitation, biomeData.temperature);
    }

    #region BiomeCenters

    public static Vector2Int BiomePosition(Vector3Int worldPosition)
    {
        Vector2Int biomePosition = new Vector2Int(
            (worldPosition.x / BiomeSize) * BiomeSize,
            (worldPosition.z / BiomeSize) * BiomeSize);
        return biomePosition;
    }

    public void GenerateBiomePoints(Vector3Int generateAround, int radius)
    {
        Vector2Int biomePosition = BiomePosition(generateAround);
        
        if (currentBiomePosition == biomePosition && !firstGeneration)
        {
            Debug.Log("In same biome, don't calculate new centers");
            return;
        }

        firstGeneration = false;
        
        Debug.Log("In new biome, calculate new centers");
        currentBiomePosition = biomePosition;

        biomeCenterPoints = CalculateBiomeCenters(currentBiomePosition, radius);
        
        // if(useDomainWarping)
        // {
        //     for (int i = 0; i < biomeCenterPoints.Count; i++)
        //     {
        //         Vector2Int domainOffset = biomeDomainWarping.GenerateDomainOffsetInt(
        //             biomeCenterPoints[i].x + World.WorldOffset.x,
        //             biomeCenterPoints[i].y + World.WorldOffset.z);
        //         biomeCenterPoints[i] += new Vector2Int(domainOffset.x, domainOffset.y);
        //     }
        // }
        
    }

    private List<Vector2Int> CalculateBiomeCenters(Vector2Int biomePosition, int radius)
    {
        // Set capacity to number of points to add to avoid resize
        int capacity1D = (radius * 2) + 1;
        int capacity2D = capacity1D * capacity1D;
        List<Vector2Int> centerPoints = new List<Vector2Int>(capacity2D);

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                centerPoints.Add(new Vector2Int(biomePosition.x + x * BiomeSize, biomePosition.y + z * BiomeSize));
            }
        }

        return centerPoints;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0f, 1f, 0.4f);
        

        foreach (Vector2Int biomeCenterPoint in biomeCenterPoints)
        {
            Vector3 centerPoint = new Vector3(biomeCenterPoint.x, 100f, biomeCenterPoint.y); 
            Gizmos.DrawCube(centerPoint, new Vector3(1f, 200f, 1f));
        }
    }
#endif
}
