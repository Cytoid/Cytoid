using System;
using System.Collections.Generic;
using System.IO;
using NLayer;
using UnityEngine;

public sealed class NLayerMemoryLoader : IDisposable
{
    private readonly byte[] bytes;
    private readonly string filename;
    private readonly List<MpegFile> createdFiles = new List<MpegFile>();
    private MpegFile file;

    public NLayerMemoryLoader(byte[] bytes, string filename)
    {
        this.bytes = bytes;
        this.filename = filename;
        file = CreateFile();
    }

    public AudioClip LoadAudioClip()
    {
        return AudioClip.Create(filename,
            (int) (file.Length / sizeof(float) / file.Channels),
            file.Channels,
            file.SampleRate,
            true,
            data => file.ReadSamples(data, 0, data.Length),
            position =>
            {
                var f = CreateFile();
                f.Time = TimeSpan.FromSeconds(position * 1.0f / f.SampleRate);
                file = f;
            });
    }

    private MpegFile CreateFile()
    {
        var result = new MpegFile(new MemoryStream(bytes, false));
        createdFiles.Add(result);
        return result;
    }

    public void Dispose()
    {
        createdFiles.ForEach(it => it.Dispose());
        createdFiles.Clear();
        file = null;
    }
}
