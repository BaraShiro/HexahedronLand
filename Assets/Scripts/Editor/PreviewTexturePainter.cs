using System;
using UnityEditor;
using UnityEngine;

public static class PreviewTexturePainter
{
    public const int PreviewTextureSize = 256;

    public static float GetPixelValue(int x, int z, NoiseSettings noiseSettings)
    {
        float noise = NoiseGenerator.OctaveSimplexNoiseBurstCompiled(x + World.WorldOffset.x, z + World.WorldOffset.z, noiseSettings.settingsData);
        if (noiseSettings.useEasing)
        {
            noise = NoiseGenerator.Redistribution(noise, noiseSettings.redistributionModifier, noiseSettings.exponent);
            // Func<float, float> EasingFunction = Easing.GetEasingFunction(noiseSettings.easingFunction);
            // noise = EasingFunction(noise * noiseSettings.redistributionModifier);
        }

        return noise;
    }

    public static Texture2D GetNewPreviewTexture()
    {
        return new Texture2D(PreviewTextureSize, PreviewTextureSize, TextureFormat.RGBA32, false);
    }
}
