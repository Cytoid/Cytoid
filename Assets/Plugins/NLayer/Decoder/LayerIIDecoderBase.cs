/*
 * NLayer - A C# MPEG1/2/2.5 audio decoder
 * 
 */

using System;

namespace NLayer.Decoder
{
    // Layers I & II are basically identical...  Layer II adds sample grouping, per subband allocation schemes, and granules
    // Because of this fact, we can use the same decoder for both
    abstract class LayerIIDecoderBase : LayerDecoderBase
    {
        protected const int SSLIMIT = 12;

        static protected bool GetCRC(MpegFrame frame, int[] rateTable, int[][] allocLookupTable, bool readScfsiBits, ref uint crc)
        {
            // ugh...  we basically have to re-implement the allocation logic here.

            // keep up with how many active subbands we need to read selection info for
            var scfsiBits = 0;

            // only read as many subbands as we actually need; pay attention to the intensity stereo subbands
            var subbandCount = rateTable.Length;
            var jsbound = subbandCount;
            if (frame.ChannelMode == MpegChannelMode.JointStereo)
            {
                jsbound = frame.ChannelModeExtension * 4 + 4;
            }

            // read the full stereo subbands
            var channels = frame.ChannelMode == MpegChannelMode.Mono ? 1 : 2;
            var sb = 0;
            for (; sb < jsbound; sb++)
            {
                var bits = allocLookupTable[rateTable[sb]][0];
                for (int ch = 0; ch < channels; ch++)
                {
                    var alloc = frame.ReadBits(bits);
                    if (alloc > 0) scfsiBits += 2;

                    MpegFrame.UpdateCRC(alloc, bits, ref crc);
                }
            }

            // read the intensity stereo subbands
            for (; sb < subbandCount; sb++)
            {
                var bits = allocLookupTable[rateTable[sb]][0];

                var alloc = frame.ReadBits(bits);
                if (alloc > 0) scfsiBits += channels * 2;

                MpegFrame.UpdateCRC(alloc, bits, ref crc);
            }

            // finally, read the scalefac selection bits
            if (readScfsiBits)
            {
                while (scfsiBits >= 2)
                {
                    MpegFrame.UpdateCRC(frame.ReadBits(2), 2, ref crc);
                    scfsiBits -= 2;
                }
            }

            return true;
        }

        #region Lookup Tables

        // this is from the formula: C = 1 / (1 / (1 << (Bits / 2 + Bits % 2 - 1)) + .5f)
        // index by real bits (Bits / 2 + Bits % 2 - 1)
        static readonly float[] _groupedC = { 0, 0, 1.33333333333f, 1.60000000000f, 1.77777777777f };

        // these are always -0.5
        // index by real bits (Bits / 2 + Bits % 2 - 1)
        static readonly float[] _groupedD = { 0, 0, -0.5f, -0.5f, -0.5f };

        // this is from the formula: 1 / (1 - (1f / (1 << Bits)))
        // index by bits
        static readonly float[] _C = {
                                         0.00000000000f,
                                         0.00000000000f, 1.33333333333f, 1.14285714286f, 1.06666666666f, 1.03225806452f, 1.01587301587f, 1.00787401575f, 1.00392156863f,
                                         1.00195694716f, 1.00097751711f, 1.00048851979f, 1.00024420024f, 1.00012208522f, 1.00006103888f, 1.00003051851f, 1.00001525902f
                                     };

        // this is from the formula: 1f / (1 << Bits - 1) - 1
        // index by bits
        static readonly float[] _D = {
                                         0.00000000000f - 0f,
                                         0.00000000000f - 0f, 0.50000000000f - 1f, 0.25000000000f - 1f, 0.12500000000f - 1f, 0.062500000000f - 1f, 0.03125000000f - 1f, 0.01562500000f - 1f, 0.00781250000f - 1f,
                                         0.00390625000f - 1f, 0.00195312500f - 1f, 0.00097656250f - 1f, 0.00048828125f - 1f, 0.000244140630f - 1f, 0.00012207031f - 1f, 0.00006103516f - 1f, 0.00003051758f - 1f
                                     };

        // this is from a (really annoying) formula:
        //   x = Math.Pow(4, 1 / ((2 << (idx % 3) + 1) - (idx % 3))) / (1 << (idx / 3))
        // Basically...
        //   [0] = Math.Pow(4, 1 / 2), [1] = Math.Pow(4, 1 / 3), [2] = Math.Pow(4, 1 / 6)
        //   For every remaining element, calculate (in order): [idx] = [idx - 3] / 2
        static readonly float[] _denormalMultiplier = {
                                                          2.00000000000000f, 1.58740105196820f, 1.25992104989487f, 1.00000000000000f, 0.79370052598410f, 0.62996052494744f, 0.50000000000000f, 0.39685026299205f,
                                                          0.31498026247372f, 0.25000000000000f, 0.19842513149602f, 0.15749013123686f, 0.12500000000000f, 0.09921256574801f, 0.07874506561843f, 0.06250000000000f,
                                                          0.04960628287401f, 0.03937253280921f, 0.03125000000000f, 0.02480314143700f, 0.01968626640461f, 0.01562500000000f, 0.01240157071850f, 0.00984313320230f,
                                                          0.00781250000000f, 0.00620078535925f, 0.00492156660115f, 0.00390625000000f, 0.00310039267963f, 0.00246078330058f, 0.00195312500000f, 0.00155019633981f,
                                                          0.00123039165029f, 0.00097656250000f, 0.00077509816991f, 0.00061519582514f, 0.00048828125000f, 0.00038754908495f, 0.00030759791257f, 0.00024414062500f,
                                                          0.00019377454248f, 0.00015379895629f, 0.00012207031250f, 0.00009688727124f, 0.00007689947814f, 0.00006103515625f, 0.00004844363562f, 0.00003844973907f,
                                                          0.00003051757813f, 0.00002422181781f, 0.00001922486954f, 0.00001525878906f, 0.00001211090890f, 0.00000961243477f, 0.00000762939453f, 0.00000605545445f,
                                                          0.00000480621738f, 0.00000381469727f, 0.00000302772723f, 0.00000240310869f, 0.00000190734863f, 0.00000151386361f, 0.00000120155435f, 0.00000095367432f
                                                      };

        #endregion

        int _channels, _jsbound, _granuleCount;
        int[][] _allocLookupTable, _scfsi, _samples;
        int[][][] _scalefac;
        float[] _polyPhaseBuf;

        int[][] _allocation;

        protected LayerIIDecoderBase(int[][] allocLookupTable, int granuleCount)
            : base()
        {
            _allocLookupTable = allocLookupTable;
            _granuleCount = granuleCount;

            _allocation = new int[][] { new int[SBLIMIT], new int[SBLIMIT] };
            _scfsi = new int[][] { new int[SBLIMIT], new int[SBLIMIT] };
            _samples = new int[][] { new int[SBLIMIT * SSLIMIT * _granuleCount], new int[SBLIMIT * SSLIMIT * _granuleCount] };

            // NB: ReadScaleFactors(...) requires all three granules, even in Layer I
            _scalefac = new int[][][] { new int[3][], new int[3][] };
            for (int i = 0; i < 3; i++)
            {
                _scalefac[0][i] = new int[SBLIMIT];
                _scalefac[1][i] = new int[SBLIMIT];
            }

            _polyPhaseBuf = new float[SBLIMIT];
        }

        internal override int DecodeFrame(IMpegFrame frame, float[] ch0, float[] ch1)
        {
            InitFrame(frame);

            var rateTable = GetRateTable(frame);

            ReadAllocation(frame, rateTable);

            for (int i = 0; i < _scfsi[0].Length; i++)
            {
                // Since Layer II has to know which subbands have energy, we use the "Layer I valid" selection to mark that energy is present.
                // That way Layer I doesn't have to do anything else.
                _scfsi[0][i] = _allocation[0][i] != 0 ? 2 : -1;
                _scfsi[1][i] = _allocation[1][i] != 0 ? 2 : -1;
            }
            ReadScaleFactorSelection(frame, _scfsi, _channels);

            ReadScaleFactors(frame);

            ReadSamples(frame);

            return DecodeSamples(ch0, ch1);
        }

        // this just reads the channel mode and set a few flags
        void InitFrame(IMpegFrame frame)
        {
            switch (frame.ChannelMode)
            {
                case MpegChannelMode.Mono:
                    _channels = 1;
                    _jsbound = SBLIMIT;
                    break;
                case MpegChannelMode.JointStereo:
                    _channels = 2;
                    _jsbound = frame.ChannelModeExtension * 4 + 4;
                    break;
                default:
                    _channels = 2;
                    _jsbound = SBLIMIT;
                    break;
            }
        }

        abstract protected int[] GetRateTable(IMpegFrame frame);

        void ReadAllocation(IMpegFrame frame, int[] rateTable)
        {
            var _subBandCount = rateTable.Length;
            if (_jsbound > _subBandCount) _jsbound = _subBandCount;

            Array.Clear(_allocation[0], 0, SBLIMIT);
            Array.Clear(_allocation[1], 0, SBLIMIT);

            int sb = 0;
            for (; sb < _jsbound; sb++)
            {
                var table = _allocLookupTable[rateTable[sb]];
                var bits = table[0];
                for (int ch = 0; ch < _channels; ch++)
                {
                    _allocation[ch][sb] = table[frame.ReadBits(bits) + 1];
                }
            }
            for (; sb < _subBandCount; sb++)
            {
                var table = _allocLookupTable[rateTable[sb]];
                _allocation[0][sb] = _allocation[1][sb] = table[frame.ReadBits(table[0]) + 1];
            }
        }

        abstract protected void ReadScaleFactorSelection(IMpegFrame frame, int[][] scfsi, int channels);

        void ReadScaleFactors(IMpegFrame frame)
        {
            for (int sb = 0; sb < SBLIMIT; sb++)
            {
                for (int ch = 0; ch < _channels; ch++)
                {
                    switch (_scfsi[ch][sb])
                    {
                        case 0:
                            // all three
                            _scalefac[ch][0][sb] = frame.ReadBits(6);
                            _scalefac[ch][1][sb] = frame.ReadBits(6);
                            _scalefac[ch][2][sb] = frame.ReadBits(6);
                            break;
                        case 1:
                            // only two (2 = 1)
                            _scalefac[ch][0][sb] =
                            _scalefac[ch][1][sb] = frame.ReadBits(6);
                            _scalefac[ch][2][sb] = frame.ReadBits(6);
                            break;
                        case 2:
                            // only one (3 = 2 = 1)
                            _scalefac[ch][0][sb] =
                            _scalefac[ch][1][sb] =
                            _scalefac[ch][2][sb] = frame.ReadBits(6);
                            break;
                        case 3:
                            // only two (3 = 2)
                            _scalefac[ch][0][sb] = frame.ReadBits(6);
                            _scalefac[ch][1][sb] =
                            _scalefac[ch][2][sb] = frame.ReadBits(6);
                            break;
                        default:
                            // none
                            _scalefac[ch][0][sb] = 63;
                            _scalefac[ch][1][sb] = 63;
                            _scalefac[ch][2][sb] = 63;
                            break;
                    }
                }
            }
        }

        void ReadSamples(IMpegFrame frame)
        {
            // load in all the data for this frame (1152 samples in this case)
            // NB: we flatten these into output order
            for (int ss = 0, idx = 0; ss < SSLIMIT; ss++, idx += SBLIMIT * (_granuleCount - 1))
            {
                for (int sb = 0; sb < SBLIMIT; sb++, idx++)
                {
                    for (int ch = 0; ch < _channels; ch++)
                    {
                        if (ch == 0 || sb < _jsbound)
                        {
                            var alloc = _allocation[ch][sb];
                            if (alloc != 0)
                            {
                                if (alloc < 0)
                                {
                                    // grouping (Layer II only, so we don't have to play with the granule count)
                                    var val = frame.ReadBits(-alloc);
                                    var levels = (1 << (-alloc / 2 + -alloc % 2 - 1)) + 1;

                                    _samples[ch][idx] = val % levels;
                                    val /= levels;
                                    _samples[ch][idx + SBLIMIT] = val % levels;
                                    _samples[ch][idx + SBLIMIT * 2] = val / levels;
                                }
                                else
                                {
                                    // non-grouping
                                    for (int gr = 0; gr < _granuleCount; gr++)
                                    {
                                        _samples[ch][idx + SBLIMIT * gr] = frame.ReadBits(alloc);
                                    }
                                }
                            }
                            else
                            {
                                // no energy...  zero out the samples
                                for (int gr = 0; gr < _granuleCount; gr++)
                                {
                                    _samples[ch][idx + SBLIMIT * gr] = 0;
                                }
                            }
                        }
                        else
                        {
                            // copy chan 0 to chan 1
                            for (int gr = 0; gr < _granuleCount; gr++)
                            {
                                _samples[1][idx + SBLIMIT * gr] = _samples[0][idx + SBLIMIT * gr];
                            }
                        }
                    }
                }
            }
        }

        int DecodeSamples(float[] ch0, float[] ch1)
        {
            // do our stereo mode setup
            var chanBufs = new float[2][];
            var startChannel = 0;
            var endChannel = _channels - 1;
            if (_channels == 1 || StereoMode == StereoMode.LeftOnly)
            {
                chanBufs[0] = ch0;
                endChannel = 0;
            }
            else if (StereoMode == StereoMode.RightOnly)
            {
                chanBufs[1] = ch0;  // this is correct... if there's only a single channel output, it goes in channel 0's buffer
                startChannel = 1;
            }
            else    // MpegStereoMode.Both or StereoMode.DownmixToMono
            {
                chanBufs[0] = ch0;
                chanBufs[1] = ch1;
            }

            int idx = 0;
            for (int ch = startChannel; ch <= endChannel; ch++)
            {
                idx = 0;
                for (int gr = 0; gr < _granuleCount; gr++)
                {
                    for (int ss = 0; ss < SSLIMIT; ss++)
                    {
                        for (int sb = 0; sb < SBLIMIT; sb++, idx++)
                        {
                            // do the dequant and the denorm; output to _polyPhaseBuf
                            // NB: Layers I & II use the same algorithm here...  Grouping changes the bit counts, but doesn't change the algo
                            //     - Up to 65534 possible values (65535 does not appear to be usable)
                            //     - All values can be handled with 16-bit logic as long as the correct C and D constants are used
                            //     - Make sure to normalize each sample to 16 bits!

                            var alloc = _allocation[ch][sb];
                            if (alloc != 0)
                            {
                                float[] c, d;
                                if (alloc < 0)
                                {
                                    alloc = -alloc / 2 + -alloc % 2 - 1;
                                    c = _groupedC;
                                    d = _groupedD;
                                }
                                else
                                {
                                    c = _C;
                                    d = _D;
                                }

                                // read sample; normalize, scale & center to [-0.999984741f..0.999984741f]; apply scalefactor
                                _polyPhaseBuf[sb] = c[alloc] * ((_samples[ch][idx] << (16 - alloc)) / 32768f + d[alloc]) * _denormalMultiplier[_scalefac[ch][gr][sb]];
                            }
                            else
                            {
                                // no transmitted energy...
                                _polyPhaseBuf[sb] = 0f;
                            }
                        }

                        // do the polyphase output for this channel, section, and granule
                        base.InversePolyPhase(ch, _polyPhaseBuf);
                        Array.Copy(_polyPhaseBuf, 0, chanBufs[ch], idx - SBLIMIT, SBLIMIT);
                    }
                }
            }

            if (_channels == 2 && StereoMode == NLayer.StereoMode.DownmixToMono)
            {
                for (int i = 0; i < idx; i++)
                {
                    ch0[i] = (ch0[i] + ch1[i]) / 2;
                }
            }

            return idx;
        }
    }
}
