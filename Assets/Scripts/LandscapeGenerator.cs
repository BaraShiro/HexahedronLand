using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

public class LandscapeGenerator : MonoBehaviour
{
    private readonly ref struct BiomeData
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
    
    private readonly ref struct BiomeCenters
    {
        public readonly BiomeData northWestBiome;
        public readonly BiomeData northEastBiome;
        public readonly BiomeData southWestBiome;
        public readonly BiomeData southEastBiome;

        public BiomeCenters(BiomeData northWestBiome, BiomeData northEastBiome, BiomeData southWestBiome, BiomeData southEastBiome)
        {
            this.northWestBiome = northWestBiome;
            this.northEastBiome = northEastBiome;
            this.southWestBiome = southWestBiome;
            this.southEastBiome = southEastBiome;
        }

        public BiomeData GetClosest()
        {
            BiomeData northData = northWestBiome.distance < northEastBiome.distance ? northWestBiome : northEastBiome;
            BiomeData southData = southWestBiome.distance < southEastBiome.distance ? southWestBiome : southEastBiome;

            return northData.distance < southData.distance ? northData : southData;
        }

        public override string ToString()
        {
            return $"NW: pos: {northWestBiome.center} dist: {northWestBiome.distance}, " +
                   $"NE: pos: {northEastBiome.center} dist: {northEastBiome.distance}, " +
                   $"SW: pos: {southWestBiome.center} dist: {southWestBiome.distance}, " +
                   $"SE: pos: {southEastBiome.center} dist: {southEastBiome.distance}";
        }
    }

    [SerializeField] private NoiseSettings biomeTemperatureNoiseSettings;
    [SerializeField] private NoiseSettings biomePrecipitationNoiseSettings;
    [SerializeField] private DomainWarping biomeDomainWarping;
    [SerializeReference] private BiomeClimateData biomeClimateData = new BiomeClimateData(1,1);
    public bool useDomainWarping = true;

    private const int BiomeSize = Chunk.ChunkData.ChunkHorizontalSize * Chunk.ChunkData.ChunkHorizontalSize;
    // TODO: BiomeRadius should be relative to draw distance, if blocks are created outside of the biome grid behaviour is undefined
    private const int BiomeRadius = 2;

    private Vector2Int currentBiomePosition = Vector2Int.zero;
    private List<Vector2Int> biomeCenterPoints = new List<Vector2Int>();
    private bool firstGeneration = true;

    private void SelectBiome(Vector2Int worldPosition, out SurfaceBiomeGenerator biomeGenerator, out int rockHeight, out int dirtHeight)
    {
        if(useDomainWarping)
        {
            Vector2Int domainOffset = biomeDomainWarping.GenerateDomainOffsetInt(worldPosition.x + World.WorldOffset.x, worldPosition.y + World.WorldOffset.z);
            worldPosition += new Vector2Int(domainOffset.x, domainOffset.y);
        }
        
        CalculateBiomeData(worldPosition, out BiomeCenters biomeCenters);
        
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

        biomeGenerator = SelectBiomeGenerator(biomeCenters.GetClosest());
        rockHeight = Mathf.RoundToInt(weightedHeights.x);
        dirtHeight = Mathf.RoundToInt(weightedHeights.y);
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

    private void CalculateBiomeData(Vector2Int horizontalPosition, out BiomeCenters centers)
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

        centers = new BiomeCenters(northWestBiome, northEastBiome, southWestBiome, southEastBiome);
    }

    private ref SurfaceBiomeGenerator SelectBiomeGenerator(in BiomeData biomeData)
    {
      return ref biomeClimateData.GetBiome(biomeData.precipitation, biomeData.temperature);
    }

    #region BiomeCenters

    public static Vector2Int WorldPositionToBiomePosition(Vector3Int worldPosition)
    {
        Vector2Int biomePosition = new Vector2Int(
            (worldPosition.x / BiomeSize) * BiomeSize,
            (worldPosition.z / BiomeSize) * BiomeSize);
        return biomePosition;
    }

    public void UpdateBiomeCenterPoints(Vector3Int generateAround)
    {
        Vector2Int biomePosition = WorldPositionToBiomePosition(generateAround);
        
        if (currentBiomePosition == biomePosition && !firstGeneration)
        {
            Debug.Log("In same biome, don't calculate new centers");
            return;
        }

        firstGeneration = false;
        
        Debug.Log("In new biome, calculate new centers");
        currentBiomePosition = biomePosition;

        biomeCenterPoints = CalculateBiomeCenters(currentBiomePosition);
        
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

    private List<Vector2Int> CalculateBiomeCenters(Vector2Int biomePosition)
    {
        // Set capacity to number of points to add to avoid resize
        const int capacity1D = (BiomeRadius * 2) + 1;
        const int capacity2D = capacity1D * capacity1D;
        List<Vector2Int> centerPoints = new List<Vector2Int>(capacity2D);

        for (int x = -BiomeRadius; x <= BiomeRadius; x++)
        {
            for (int z = -BiomeRadius; z <= BiomeRadius; z++)
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
