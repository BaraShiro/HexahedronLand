using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public static class MathHelpers
{
    // https://www.geo.fu-berlin.de/en/v/soga/Geodata-analysis/geostatistics/Inverse-Distance-Weighting/index.html
    [BurstCompile]
    public static float InverseDistanceWeighting(in float2 point, in float2x4 observations, in float4 values, int beta = 1)
    {
        // If point coincides with an observation location then the observed value is returned to avoid infinite weights.
        if (point.Equals(observations.c0))
        {
            return values.x;
        }
        if (point.Equals(observations.c1))
        {
            return values.y;
        }
        if (point.Equals(observations.c2))
        {
            return values.z;
        }
        if (point.Equals(observations.c3))
        {
            return values.w;
        }
        
        double4 weights = new double4
        {
            x = math.pow(math.distance(point, observations.c0), -beta),
            y = math.pow(math.distance(point, observations.c1), -beta),
            z = math.pow(math.distance(point, observations.c2), -beta),
            w = math.pow(math.distance(point, observations.c3), -beta)
        };
        
        double weightSum = math.csum(weights);
        double weightedValueSum = math.mul(weights, values);
        double result = weightedValueSum / weightSum;
        
        return (float) result;
    }

    [BurstCompile]
    public static void InverseDistanceWeighting(in float2 point, in float2x4 observations, in float4x2 values, out float2 result, int beta = 1)
    {
        // If point coincides with an observation location then the observed value is returned to avoid infinite weights.
        if (point.Equals(observations.c0))
        {
            result = new float2(values.c0.x, values.c1.x);
            return;
        }
        if (point.Equals(observations.c1))
        {
            result = new float2(values.c0.y, values.c1.y);
            return;
        }
        if (point.Equals(observations.c2))
        {
            result = new float2(values.c0.z, values.c1.z);
            return;
        }
        if (point.Equals(observations.c3))
        {
            result = new float2(values.c0.w, values.c1.w);
            return;
        }

        double4 weights = new double4
        {
            x = math.pow(math.distance(point, observations.c0), -beta),
            y = math.pow(math.distance(point, observations.c1), -beta),
            z = math.pow(math.distance(point, observations.c2), -beta),
            w = math.pow(math.distance(point, observations.c3), -beta)
        };

        double weightSum = math.csum(weights);
        double weightedValueC0Sum = math.mul(weights, values.c0);
        double weightedValueC1Sum = math.mul(weights, values.c1);
        result = new float2((float)(weightedValueC0Sum / weightSum), (float)(weightedValueC1Sum / weightSum));
    }
    
    [BurstCompile]
    public static float InverseDistanceSquaredWeighting(in float2 point, in float2x4 observations, in float4 values)
    {
        return InverseDistanceWeighting(in point, in observations, in values, beta: 2);
    }
    
    [BurstCompile]
    public static void InverseDistanceSquaredWeighting(in float2 point, in float2x4 observations, in float4x2 values, out float2 result)
    {
        InverseDistanceWeighting(in point, in observations, in values, out result, beta: 2);
    }
    
    [BurstCompile]
    public static float InverseDistanceCubedWeighting(in float2 point, in float2x4 observations, in float4 values)
    {
        return InverseDistanceWeighting(in point, in observations, in values, beta: 3);
    }
    
    [BurstCompile]
    public static void InverseDistanceCubedWeighting(in float2 point, in float2x4 observations, in float4x2 values, out float2 result)
    {
        InverseDistanceWeighting(in point, in observations, in values, out result, beta: 3);
    }
}

