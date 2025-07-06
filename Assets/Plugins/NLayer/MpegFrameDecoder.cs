using System;

namespace NLayer
{
    public class MpegFrameDecoder
    {
        Decoder.LayerIDecoder _layerIDecoder;
        Decoder.LayerIIDecoder _layerIIDecoder;
        Decoder.LayerIIIDecoder _layerIIIDecoder;

        float[] _eqFactors;

        // channel buffers for getting data out of the decoders...
        // we do it this way so the stereo interleaving code is in one place: DecodeFrameImpl(...)
        // if we ever add support for multi-channel, we'll have to add a pass after the initial
        //  stereo decode (since multi-channel basically uses the stereo channels as a reference)
        float[] _ch0, _ch1;

        public MpegFrameDecoder()
        {
            _ch0 = new float[1152];
            _ch1 = new float[1152];
        }

        /// <summary>
        /// Set the equalizer.
        /// </summary>
        /// <param name="eq">The equalizer, represented by an array of 32 adjustments in dB.</param>
        public void SetEQ(float[] eq)
        {
            if (eq != null)
            {
                var factors = new float[32];
                for (int i = 0; i < eq.Length; i++)
                {
                    // convert from dB -> scaling
                    factors[i] = (float)Math.Pow(2, eq[i] / 6);
                }
                _eqFactors = factors;
            }
            else
            {
                _eqFactors = null;
            }
        }

        /// <summary>
        /// Stereo mode used in decoding.
        /// </summary>
        public StereoMode StereoMode { get; set; }

        /// <summary>
        /// Decode the Mpeg frame into provided buffer. Do exactly the same as <see cref="DecodeFrame(IMpegFrame, float[], int)"/>
        /// except that the data is written in type as byte array, while still representing single-precision float (in local endian).
        /// </summary>
        /// <param name="frame">The Mpeg frame to be decoded.</param>
        /// <param name="dest">Destination buffer. Decoded PCM (single-precision floating point array) will be written into it.</param>
        /// <param name="destOffset">Writing offset on the destination buffer.</param>
        /// <returns></returns>
        public int DecodeFrame(IMpegFrame frame, byte[] dest, int destOffset)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            if (dest == null) throw new ArgumentNullException("dest");
            if (destOffset % 4 != 0) throw new ArgumentException("Must be an even multiple of 4", "destOffset");

            var bufferAvailable = (dest.Length - destOffset) / 4;
            if (bufferAvailable < (frame.ChannelMode == MpegChannelMode.Mono ? 1 : 2) * frame.SampleCount)
            {
                throw new ArgumentException("Buffer not large enough!  Must be big enough to hold the frame's entire output.  This is up to 9,216 bytes.", "dest");
            }

            return DecodeFrameImpl(frame, dest, destOffset / 4) * 4;
        }

        /// <summary>
        /// Decode the Mpeg frame into provided buffer.
        /// Result varies with different <see cref="StereoMode"/>:
        /// <list type="bullet">
        /// <item>
        /// <description>For <see cref="NLayer.StereoMode.Both"/>, sample data on both two channels will occur in turn (left first).</description>
        /// </item>
        /// <item>
        /// <description>For <see cref="NLayer.StereoMode.LeftOnly"/> and <see cref="NLayer.StereoMode.RightOnly"/>, only data on
        /// specified channel will occur.</description>
        /// </item>
        /// <item>
        /// <description>For <see cref="NLayer.StereoMode.DownmixToMono"/>, two channels will be down-mixed into single channel.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="frame">The Mpeg frame to be decoded.</param>
        /// <param name="dest">Destination buffer. Decoded PCM (single-precision floating point array) will be written into it.</param>
        /// <param name="destOffset">Writing offset on the destination buffer.</param>
        /// <returns></returns>
        public int DecodeFrame(IMpegFrame frame, float[] dest, int destOffset)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            if (dest == null) throw new ArgumentNullException("dest");

            if (dest.Length - destOffset < (frame.ChannelMode == MpegChannelMode.Mono ? 1 : 2) * frame.SampleCount)
            {
                throw new ArgumentException("Buffer not large enough!  Must be big enough to hold the frame's entire output.  This is up to 2,304 elements.", "dest");
            }

            return DecodeFrameImpl(frame, dest, destOffset);
        }

        int DecodeFrameImpl(IMpegFrame frame, Array dest, int destOffset)
        {
            frame.Reset();

            Decoder.LayerDecoderBase curDecoder = null;
            switch (frame.Layer)
            {
                case MpegLayer.LayerI:
                    if (_layerIDecoder == null)
                    {
                        _layerIDecoder = new Decoder.LayerIDecoder();
                    }
                    curDecoder = _layerIDecoder;
                    break;
                case MpegLayer.LayerII:
                    if (_layerIIDecoder == null)
                    {
                        _layerIIDecoder = new Decoder.LayerIIDecoder();
                    }
                    curDecoder = _layerIIDecoder;
                    break;
                case MpegLayer.LayerIII:
                    if (_layerIIIDecoder == null)
                    {
                        _layerIIIDecoder = new Decoder.LayerIIIDecoder();
                    }
                    curDecoder = _layerIIIDecoder;
                    break;
            }

            if (curDecoder != null)
            {
                curDecoder.SetEQ(_eqFactors);
                curDecoder.StereoMode = StereoMode;

                var cnt = curDecoder.DecodeFrame(frame, _ch0, _ch1);

                if (frame.ChannelMode == MpegChannelMode.Mono)
                {
                    Buffer.BlockCopy(_ch0, 0, dest, destOffset * sizeof(float), cnt * sizeof(float));
                }
                else
                {
                    // This is kinda annoying...  if we're doing a downmix, we should technically only output a single channel
                    //  The problem is, our caller is probably expecting stereo output.  Grrrr....

                    // We use Buffer.BlockCopy here because we don't know dest's type, but do know it's big enough to do the copy
                    for (int i = 0; i < cnt; i++)
                    {
                        Buffer.BlockCopy(_ch0, i * sizeof(float), dest, destOffset * sizeof(float), sizeof(float));
                        ++destOffset;
                        Buffer.BlockCopy(_ch1, i * sizeof(float), dest, destOffset * sizeof(float), sizeof(float));
                        ++destOffset;
                    }
                    cnt *= 2;
                }

                return cnt;
            }

            return 0;
        }

        /// <summary>
        /// Reset the decoder.
        /// </summary>
        public void Reset()
        {
            // the synthesis filters need to be cleared
            if (_layerIDecoder != null)
            {
                _layerIDecoder.ResetForSeek();
            }
            if (_layerIIDecoder != null)
            {
                _layerIIDecoder.ResetForSeek();
            }
            if (_layerIIIDecoder != null)
            {
                _layerIIIDecoder.ResetForSeek();
            }
        }
    }
}
