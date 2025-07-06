/*
 * NLayer - A C# MPEG1/2/2.5 audio decoder
 * 
 */

using System;

namespace NLayer.Decoder
{
    // there's not much we have to do here... table selection, granule count, scalefactor selection
    class LayerIIDecoder : LayerIIDecoderBase
    {
        static internal bool GetCRC(MpegFrame frame, ref uint crc)
        {
            return LayerIIDecoderBase.GetCRC(frame, SelectTable(frame), _allocLookupTable, true, ref crc);
        }

        // figure out which rate table to use...  basically, high-rate full, high-rate limited, low-rate limited, low-rate minimal, and LSF.
        static int[] SelectTable(IMpegFrame frame)
        {
            var bitRatePerChannel = (frame.BitRate / (frame.ChannelMode == MpegChannelMode.Mono ? 1 : 2)) / 1000;

            if (frame.Version == MpegVersion.Version1)
            {
                if ((bitRatePerChannel >= 56 && bitRatePerChannel <= 80) || (frame.SampleRate == 48000 && bitRatePerChannel >= 56))
                {
                    return _rateLookupTable[0];   // high-rate, 27 subbands
                }
                else if (frame.SampleRate != 48000 && bitRatePerChannel >= 96)
                {
                    return _rateLookupTable[1];   // high-rate, 30 subbands
                }
                else if (frame.SampleRate != 32000 && bitRatePerChannel <= 48)
                {
                    return _rateLookupTable[2];   // low-rate, 8 subbands
                }
                else
                {
                    return _rateLookupTable[3];   // low-rate, 12 subbands
                }
            }
            else
            {
                return _rateLookupTable[4];   // lsf, 30 subbands
            }
        }

        // this table tells us which allocation lookup list to use for each subband
        // note that each row has the same number of elements as there are subbands for that type...
        static readonly int[][] _rateLookupTable = {
                                                       //          0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29
                                                       new int[] { 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },             // high-rate, 27 subbands
                                                       new int[] { 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },    // high-rate, 30 subbands
                                                       new int[] { 4, 4, 5, 5, 5, 5, 5, 5 },                                                                      // low-rate, 7 subbands
                                                       new int[] { 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 },                                                          // low-rate, 12 subbands
                                                       new int[] { 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 },    // lsf, 30 subbands
                                                   };

        // this tells the decode logic: a) how many bits per allocation, and b) how many bits per sample for the give allocation value
        //  if negative, read -x bits and handle as a group
        static readonly int[][] _allocLookupTable = {
                                                        //       bits   0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15
                                                        new int[] { 2,  0, -5, -7, 16 },                                                 // 0 (II)
                                                        new int[] { 3,  0, -5, -7,  3,-10,  4,  5, 16 },                                 // 1 (II)
                                                        new int[] { 4,  0, -5, -7,  3,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 16 }, // 2 (II)
                                                        new int[] { 4,  0, -5,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16 }, // 3 (II)
                                                        new int[] { 4,  0, -5, -7,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 }, // 4 (II, 4, 4 bits per alloc)
                                                        new int[] { 3,  0, -5, -7,-10,  4,  5,  6,  9 },                                 // 5 (II, 4, 3 bits per alloc)
                                                        new int[] { 4,  0, -5, -7,  3,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14 }, // 6 (II)
                                                        new int[] { 2,  0, -5, -7,  3 },                                                 // 7 (II, 4, 2 bits per alloc)
                                                    };

        internal LayerIIDecoder() : base(_allocLookupTable, 3) { }

        protected override int[] GetRateTable(IMpegFrame frame)
        {
            return SelectTable(frame);
        }

        protected override void ReadScaleFactorSelection(IMpegFrame frame, int[][] scfsi, int channels)
        {
            // we'll never have more than 30 active subbands
            for (int sb = 0; sb < 30; sb++)
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    if (scfsi[ch][sb] == 2)
                    {
                        scfsi[ch][sb] = frame.ReadBits(2);
                    }
                }
            }
        }
    }
}
