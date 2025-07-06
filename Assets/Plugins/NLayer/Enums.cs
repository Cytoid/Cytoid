using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NLayer
{
    public enum MpegVersion
    {
        Unknown = 0,
        Version1 = 10,
        Version2 = 20,
        Version25 = 25,
    }

    public enum MpegLayer
    {
        Unknown = 0,
        LayerI = 1,
        LayerII = 2,
        LayerIII = 3,
    }

    public enum MpegChannelMode
    {
        Stereo,
        JointStereo,
        DualChannel,
        Mono,
    }

    public enum StereoMode
    {
        Both,
        LeftOnly,
        RightOnly,
        DownmixToMono,
    }
}
