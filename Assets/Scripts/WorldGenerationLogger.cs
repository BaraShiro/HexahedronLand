using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

public static class WorldGenerationLogger
{
    public static readonly ConcurrentBag<long> SelectBiomeTicks = new ConcurrentBag<long>();
    public static readonly ConcurrentBag<long> HandleLayerTicks = new ConcurrentBag<long>();
    
    public static string GetSelectBiomeDetails => $"Count: {SelectBiomeTicks.Count} Sum: {SelectBiomeTicks.Sum()} Average: {(SelectBiomeTicks.IsEmpty ? 0.0 : SelectBiomeTicks.Average())}";
    public static string GetHandleLayerDetails => $"Count: {HandleLayerTicks.Count} Sum: {HandleLayerTicks.Sum()} Average: {(HandleLayerTicks.IsEmpty ? 0.0 : HandleLayerTicks.Average())}";
    
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

