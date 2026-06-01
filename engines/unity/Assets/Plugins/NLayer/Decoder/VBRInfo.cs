namespace NLayer.Decoder
{
    class VBRInfo
    {
        internal VBRInfo() { }

        internal int SampleCount { get; set; }
        internal int SampleRate { get; set; }
        internal int Channels { get; set; }
        internal int VBRFrames { get; set; }
        internal int VBRBytes { get; set; }
        internal int VBRQuality { get; set; }
        internal int VBRDelay { get; set; }

        internal long VBRStreamSampleCount
        {
            get
            {
                // we assume the entire stream is consistent wrt samples per frame
                return VBRFrames * SampleCount;
            }
        }

        internal int VBRAverageBitrate
        {
            get
            {
                return (int)((VBRBytes / (VBRStreamSampleCount / (double)SampleRate)) * 8);
            }
        }
    }
}
