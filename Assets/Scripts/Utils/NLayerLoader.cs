using System.IO;
using NLayer;
using UnityEngine;

// Credits: https://github.com/r2123b/Load-Mp3-into-Audioclip
public static class NLayerLoader
{
    public static AudioClip LoadMpeg(string filePath)
    {
        filePath = filePath.Replace("file://", "");
        var filename = Path.GetFileNameWithoutExtension(filePath);
        var file = new MpegFile(filePath);
        var audioClip = AudioClip.Create(filename,
            (int) (file.Length / sizeof(float) / file.Channels),
            file.Channels,
            file.SampleRate,
            true,
            data => file.ReadSamples(data, 0, data.Length),
            position => file = new MpegFile(filePath));
        return audioClip;
    }
}