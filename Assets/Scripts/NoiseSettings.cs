using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[CreateAssetMenu(fileName = "noiseSettings", menuName = "Data/Noise Settings")]
public class NoiseSettings : ScriptableObject
{
    [Serializable]
    public struct SettingsData
    {
        public int3 offset;
        public float noiseScale;
        public int octaves;
        public float persistence;

        public SettingsData(int3 offset, float noiseScale, int octaves, float persistence)
        {
            this.offset = offset;
            this.noiseScale = noiseScale;
            this.octaves = octaves;
            this.persistence = persistence;
        }
    }
    
    public SettingsData settingsData = new SettingsData(int3.zero, 0.1f, 1, 0.5f);

    public bool useEasing = false;
    public float redistributionModifier = 1.2f;
    public float exponent = 1f;
    public Easing.EasingFunc easingFunction = Easing.EasingFunc.InSine;
}

