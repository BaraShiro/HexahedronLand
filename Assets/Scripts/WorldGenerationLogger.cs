using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

public static class WorldGenerationLogger
{

    private const long ns = 1000000000;
    private const long μs = 1000000;
    private const long ms = 1000;
    public static readonly ConcurrentBag<long> SelectBiomeTicks = new ConcurrentBag<long>();
    public static readonly ConcurrentBag<long> HandleLayerTicks = new ConcurrentBag<long>();
    
    public static string GetSelectBiomeDetails => $"Columns: {SelectBiomeTicks.Count}, Total time: {(ms * SelectBiomeTicks.Sum()) / Stopwatch.Frequency} ms, Average time: {(SelectBiomeTicks.IsEmpty ? 0.0 : (SelectBiomeTicks.Average() / Stopwatch.Frequency) * μs)} μs";
    public static string GetHandleLayerDetails => $"Columns: {HandleLayerTicks.Count}, Total time: {(ms * HandleLayerTicks.Sum()) / Stopwatch.Frequency} ms, Average time: {(HandleLayerTicks.IsEmpty ? 0.0 : (HandleLayerTicks.Average() / Stopwatch.Frequency) * μs)} μs";
    
    private static readonly ConcurrentQueue<string> Log = new ConcurrentQueue<string>();

    public static bool logToDebug = true;

    public static void ClearChunkGenerationDetails()
    {
        SelectBiomeTicks.Clear();
        HandleLayerTicks.Clear();
    }
    
    public static void AddStepToLog(object sender, World.WorldGenerationStepEventArgs e)
    {
        Log.Enqueue(e.Message);
        
        if (logToDebug)
        {
            Debug.Log(e.Message);
        }
    }

    public static void WorldGenerationStart(object sender, EventArgs e)
    {
        ClearLog();
    }
    
    public static string GetFormattedLog()
    {
        string formattedLog = "";

        foreach (string l in Log)
        {
            formattedLog += l + "\n";
        }
        
        return formattedLog;
    }

    public static int GetLogLength() => Log.Count;

    public static void ClearLog() => Log.Clear();
}

