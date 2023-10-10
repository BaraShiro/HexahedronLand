using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public static class NoiseGenerator
{
    public static float OctaveSimplexNoise(float x, float y, float z, in NoiseSettings.SettingsData settings)
    {
        x *= settings.noiseScale;
        y *= settings.noiseScale;
        z *= settings.noiseScale;

        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float amplitudeSum = 0f;

        for (int i = 0; i < settings.octaves; i++)
        {
            float3 position = new float3(
                (settings.offset.x + x) * frequency,
                (settings.offset.y + y) * frequency,
                (settings.offset.z + z) * frequency
            );
            total += ((noise.snoise(position) + 1f) * 0.5f) * amplitude;

            amplitudeSum += amplitude;

            amplitude *= settings.persistence;
            frequency *= 2f;
        }

        return total / amplitudeSum;
    }
    
    public static float OctaveSimplexNoise(float x, float z, in NoiseSettings.SettingsData settings)
    {
        x *= settings.noiseScale;
        z *= settings.noiseScale;

        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float amplitudeSum = 0f;

        for (int i = 0; i < settings.octaves; i++)
        {
            float2 position = new float2(
                (settings.offset.x + x) * frequency,
                (settings.offset.z + z) * frequency
            );
            total += ((noise.snoise(position) + 1f) * 0.5f) * amplitude;

            amplitudeSum += amplitude;

            amplitude *= settings.persistence;
            frequency *= 2f;
        }

        return total / amplitudeSum;
    }
    
    [BurstCompile]
    public static float OctaveSimplexNoiseBurstCompiled(float x, float z, in NoiseSettings.SettingsData settings)
    {
        x *= settings.noiseScale;
        z *= settings.noiseScale;
    
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float amplitudeSum = 0f;
    
        for (int i = 0; i < settings.octaves; i++)
        {
            float2 position = new float2(
                (settings.offset.x + x) * frequency,
                (settings.offset.z + z) * frequency
            );
            total += ((noise.snoise(position) + 1f) * 0.5f) * amplitude;
    
            amplitudeSum += amplitude;
    
            amplitude *= settings.persistence;
            frequency *= 2f;
        }
    
        return total / amplitudeSum;
    }

    public static float SimplexNoise(float x, float y, float z, in NoiseSettings.SettingsData settings)
    {
        float3 position = new float3(
            settings.offset.x + x,
            settings.offset.y + y,
            settings.offset.z + z
        );
        return (noise.snoise(position) + 1f) * 0.5f;
    }
    
    public static float SimplexNoise(float x, float z, in NoiseSettings.SettingsData settings)
    {
        float2 position = new float2(
            settings.offset.x + x,
            settings.offset.z + z
        );
        return (noise.snoise(position) + 1f) * 0.5f;
    }
    
    [BurstCompile]
    public static float SimplexNoiseBurstCompiled(float x, float z, in NoiseSettings.SettingsData settings)
    {
        float2 position = new float2(
            settings.offset.x + x,
            settings.offset.z + z
        );
        return (noise.snoise(position) + 1f) * 0.5f;
    }

    // public static float RemapValue(float value, float initialMin, float initialMax, float outputMin, float outputMax)
    // {
    //     return outputMin + (value - initialMin) * (outputMax - outputMin) / (initialMax - initialMin);
    // }
    
    [BurstCompile]
    public static float RemapUnitInterval(float value, float outputMin, float outputMax)
    {
        //TODO: use unlerp: https://docs.unity.cn/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.unlerp.html
        return outputMin + ((value * (outputMax - outputMin)) / 1f);
    }
    
    [BurstCompile]
    public static int RemapUnitIntervalToInt(float value, float outputMin, float outputMax)
    {
        return (int) math.round(outputMin + ((value * (outputMax - outputMin)) / 1f));
    }

    [BurstCompile]
    public static float Normalize(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

    [BurstCompile]
    public static float Redistribution(float noise, float redistributionModifier, float exponent)
    {
        return math.pow(noise * redistributionModifier, exponent);
    }
}
