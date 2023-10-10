using System;
using Unity.Burst;
using Unity.Mathematics;

// Based on functions from https://easings.net/
[BurstCompile]
public static class Easing
{
    private const float C1 = 1.70158f;
    private const float C2 = C1 * 1.525f;
    private const float C3 = C1 + 1;
    public enum EasingFunc
    {
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InBack,
        OutBack,
        InOutBack
    }

    public static Func<float, float> GetEasingFunction(EasingFunc func)
    {
        return func switch
        {
            EasingFunc.InSine => InSine,
            EasingFunc.OutSine => OutSine,
            EasingFunc.InOutSine => InOutSine,
            EasingFunc.InQuad => InQuad,
            EasingFunc.OutQuad => OutQuad,
            EasingFunc.InOutQuad => InOutQuad,
            EasingFunc.InCubic => InCubic,
            EasingFunc.OutCubic => OutCubic,
            EasingFunc.InOutCubic => InOutCubic,
            EasingFunc.InQuart => InQuart,
            EasingFunc.OutQuart => OutQuart,
            EasingFunc.InOutQuart => InOutQuart,
            EasingFunc.InQuint => InQuint,
            EasingFunc.OutQuint => OutQuint,
            EasingFunc.InOutQuint => InOutQuint,
            EasingFunc.InExpo => InExpo,
            EasingFunc.OutExpo => OutExpo,
            EasingFunc.InOutExpo => InOutExpo,
            EasingFunc.InCirc => InCirc,
            EasingFunc.OutCirc => OutCirc,
            EasingFunc.InOutCirc => InOutCirc,
            EasingFunc.InBack => InBack,
            EasingFunc.OutBack => OutBack,
            EasingFunc.InOutBack => InOutBack,
            _ => value => value
        };
    }

    [BurstCompile]
    public static float InSine(float value)
    {
        return 1 - math.cos((value * math.PI) / 2);
    }
    
    [BurstCompile]
    public static float OutSine(float value)
    {
        return math.sin((value * math.PI) / 2);
    }
    
    [BurstCompile]
    public static float InOutSine(float value)
    {
        return -(math.cos(value * math.PI) - 1) / 2;
    }
    
    [BurstCompile]
    public static float InQuad(float value)
    {
        return value * value;
    }
    
    [BurstCompile]
    public static float OutQuad(float value)
    {
        return 1 - ((1 - value) * (1 - value));
    }
    
    [BurstCompile]
    public static float InOutQuad(float value)
    {
        return value < 0.5f ? 2 * value * value : 1 - (math.pow((-2 * value) + 2, 2) / 2);
    }
    
    [BurstCompile]
    public static float InCubic(float value)
    {
        return value * value * value;
    }
    
    [BurstCompile]
    public static float OutCubic(float value)
    {
        return 1 - math.pow(1 - value, 3);
    }
    
    [BurstCompile]
    public static float InOutCubic(float value)
    {
        return value < 0.5f ? 4 * value * value * value : 1 - (math.pow((-2 * value) + 2, 3) / 2);
    }
    
    [BurstCompile]
    public static float InQuart(float value)
    {
        return value * value * value * value;
    }
    
    [BurstCompile]
    public static float OutQuart(float value)
    {
        return 1 - math.pow(1 - value, 4);
    }
    
    [BurstCompile]
    public static float InOutQuart(float value)
    {
        return value < 0.5f ? 8 * value * value * value * value : 1 - (math.pow((-2 * value) + 2, 4) / 2);
    }
    
    [BurstCompile]
    public static float InQuint(float value)
    {
        return value * value * value * value * value;
    }
    
    [BurstCompile]
    public static float OutQuint(float value)
    {
        return 1 - math.pow(1 - value, 5);
    }
    
    [BurstCompile]
    public static float InOutQuint(float value)
    {
        return value < 0.5f ? 16 * value * value * value * value * value : 1 - (math.pow((-2 * value) + 2, 5) / 2);
    }
    
    [BurstCompile]
    public static float InExpo(float value)
    {
        return value == 0 ? 0 : math.pow(2, (10 * value) - 10);
    }
    
    [BurstCompile]
    public static float OutExpo(float value)
    {
        return value == 1 ? 1 : 1 - math.pow(2, -10 * value);
    }
    
    [BurstCompile]
    public static float InOutExpo(float value)
    {
        return value == 0f 
            ? 0 
            : value == 1 
                ? 1 
                : value < 0.5 
                    ? math.pow(2, (20 * value) - 10) / 2 
                    : (2 - math.pow(2, (-20 * value) + 10)) / 2;
    }
    
    [BurstCompile]
    public static float InCirc(float value)
    {
        return 1 - math.sqrt(1 - math.pow(value, 2));
    }
    
    [BurstCompile]
    public static float OutCirc(float value)
    {
        return math.sqrt(1 - math.pow( value - 1, 2));
    }
    
    [BurstCompile]
    public static float InOutCirc(float value)
    {
        return value < 0.5f 
            ? (1 - math.sqrt(1 - math.pow(2 * value, 2))) / 2 
            : (math.sqrt(1 - math.pow(-2 * value + 2, 2)) + 1) / 2;
    }
    
    [BurstCompile]
    public static float InBack(float value)
    {
        return (C3 * value * value * value) - (C1 * value * value);
    }
    
    [BurstCompile]
    public static float OutBack(float value)
    {
        return (1 + (C3 * math.pow(value - 1, 3))) + (C1 * math.pow(value - 1, 2));
    }
    
    [BurstCompile]
    public static float InOutBack(float value)
    {
        return value < 0.5f 
            ? (math.pow(2 * value, 2) * (((C2 + 1) * 2 * value) - C2 )) / 2 
            : ((math.pow((2 * value) - 2, 2) * (((C2 + 1) * ((value * 2) - 2)) + C2)) + 2) / 2;
    }

}

