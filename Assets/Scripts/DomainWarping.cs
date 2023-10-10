using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DomainWarping : MonoBehaviour
{
    public NoiseSettings noiseDomainX;
    public NoiseSettings noiseDomainZ;
    public int amplitudeX = 20;
    public int amplitudeZ = 20;

    public float GenerateDomainNoise(int x, int z, NoiseSettings defaultNoiseSettings)
    {
        Vector2 domainOffset = GenerateDomainOffset(x, z);
        return NoiseGenerator.OctaveSimplexNoiseBurstCompiled(x + domainOffset.x, z + domainOffset.y, in defaultNoiseSettings.settingsData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 GenerateDomainOffset(int x, int z)
    {
        float noiseX = NoiseGenerator.OctaveSimplexNoiseBurstCompiled(x, z, in noiseDomainX.settingsData) * amplitudeX;
        float noiseZ = NoiseGenerator.OctaveSimplexNoiseBurstCompiled(x, z, in noiseDomainZ.settingsData) * amplitudeZ;
        return new Vector2(noiseX, noiseZ);
    }

    public Vector2Int GenerateDomainOffsetInt(int x, int z)
    {
        return Vector2Int.RoundToInt(GenerateDomainOffset(x, z));
    }
}

