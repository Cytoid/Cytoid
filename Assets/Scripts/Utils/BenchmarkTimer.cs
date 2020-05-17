using System;
using UnityEngine;

public class BenchmarkTimer
{
    private readonly string description;
    private readonly DateTimeOffset startTime;
    private DateTimeOffset lastSectionTime;

    public bool Enabled { get; set; } = true;
    
    public BenchmarkTimer(string description)
    {
        this.description = description;
        startTime = DateTimeOffset.UtcNow;
        lastSectionTime = DateTimeOffset.UtcNow;
    }

    public void Time(string sectionDescription)
    {
        var time = DateTimeOffset.UtcNow - lastSectionTime;
        if (Enabled) Debug.Log($"{description} - {sectionDescription}: Finished in {time.TotalMilliseconds} ms");
        lastSectionTime = DateTimeOffset.UtcNow;
    }

    public void Time()
    {
        var span = DateTimeOffset.UtcNow - startTime;
        if (Enabled) Debug.Log($"{description}: Finished in {span.TotalMilliseconds} ms");
    }
    
}   