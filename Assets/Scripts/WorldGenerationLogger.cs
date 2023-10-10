using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

public static class WorldGenerationLogger
{
    private static readonly ConcurrentQueue<string> Log = new ConcurrentQueue<string>();

    public static bool logToDebug = true;

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

