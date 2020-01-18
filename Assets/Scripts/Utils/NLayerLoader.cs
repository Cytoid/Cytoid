using System;
using System.IO;
using NLayer;
using UnityEngine;

// Credits: https://github.com/r2123b/Load-Mp3-into-Audioclip
public class NLayerLoader
{
    private readonly string filePath;
    private readonly string filename;
    private MpegFile file;

    public NLayerLoader(string filePath)
    {
        filePath = filePath.Replace("file://", "");
        this.filePath = filePath;
        filename = Path.GetFileNameWithoutExtension(filePath);
        file = new MpegFile(filePath);
    }

    public AudioClip LoadAudioClip()
    {
        return AudioClip.Create(filename,
            (int) (file.Length / sizeof(float) / file.Channels),
             file.Channels,
            file.SampleRate,
            true,
            data => file.ReadSamples(data, 0, data.Length),
            position => file = new MpegFile(filePath));
    }

    public void Dispose()
    {
        file.Dispose();
    }
    
}