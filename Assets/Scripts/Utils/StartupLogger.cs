using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StartupLogger : SingletonMonoBehavior<StartupLogger>
{
    
    private readonly List<string> entries = new List<string>();
    private bool isInitialized;

    public void OnLogMessageReceived(string condition, string stacktrace, LogType type)
    {
        var str = $"{DateTime.Now:hh:mm:ss} [{type}] " + condition;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            str += $"\n    {stacktrace}";
        }
        entries.Add(str);
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        Application.logMessageReceived += OnLogMessageReceived;
    }

    public void Dispose()
    {
        if (!isInitialized) return;
        isInitialized = false;
        Save();
        entries.Clear();
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (isInitialized && !hasFocus)
        {
            Save();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (isInitialized && pauseStatus)
        {
            Save();
        }
    }

    private void Save()
    {
        var path = Context.UserDataPath + "/startup.log";
        try
        {
            File.WriteAllLines(path, entries);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Failed to write startup log");
        }
    }
    
}