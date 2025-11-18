using System;
using UnityEngine;

public abstract class NoiseGeneratorBase
{
    protected virtual Vector3 SampleOffset => Vector3.zero;

    // Generates octave noise
    public double FractalNoise(Vector3 point, int octaves, float persistence, float lacunarity, float baseFrequency, Vector3[] octaveOffsets = null)
    {
        double amplitude = 1.0;
        double frequency = baseFrequency;
        double maxAmplitude = 0.0;
        double total = 0.0;

        int octaveCount = Mathf.Max(1, octaves);
        for (int i = 0; i < octaveCount; i++)
        {
            Vector3 offset = octaveOffsets != null && i < octaveOffsets.Length ? octaveOffsets[i] : Vector3.zero;
            Vector3 generatorOffset = SampleOffset;
            double sample = Sample(
                point.x * frequency + offset.x + generatorOffset.x,
                point.y * frequency + offset.y + generatorOffset.y,
                point.z * frequency + offset.z + generatorOffset.z);

            total += (sample * 2.0 - 1.0) * amplitude; // Convert to [-1,1] so amplitudes can cancel each other out
            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        double normalised = total / Math.Max(1e-6, maxAmplitude);
        return (normalised + 1.0) * 0.5; // Map back into [0,1] range so it can be used as a heightmap
    }

    protected abstract double Sample(double x, double y, double z);
}
