using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NLayer.Decoder
{
    class ID3Frame : FrameBase
    {
        internal static ID3Frame TrySync(uint syncMark)
        {
            if ((syncMark & 0xFFFFFF00U) == 0x49443300)
            {
                return new ID3Frame { _version = 2 };
            }

            if ((syncMark & 0xFFFFFF00U) == 0x54414700)
            {
                if ((syncMark & 0xFF) == 0x2B)
                {
                    return new ID3Frame { _version = 1 };
                }
                else
                {
                    return new ID3Frame { _version = 0 };
                }
            }

            return null;
        }

        int _version;

        ID3Frame()
        {

        }

        protected override int Validate()
        {
            switch (_version)
            {
                case 2:
                    // v2, yay!
                    var buf = new byte[7];
                    if (Read(3, buf) == 7)
                    {
                        byte flagsMask;
                        switch (buf[0])
                        {
                            case 2:
                                flagsMask = 0x3F;
                                break;
                            case 3:
                                flagsMask = 0x1F;
                                break;
                            case 4:
                                flagsMask = 0x0F;
                                break;
                            default:
                                return -1;
                        }

                        // ignore the flags (we don't need them for the validation)

                        // get the size (7 bits per byte [MSB cleared])
                        var size = (buf[3] << 21)
                                 | (buf[4] << 14)
                                 | (buf[5] << 7)
                                 | (buf[6]);

                        // finally, check to make sure that all the right bits are cleared
                        if (!(((buf[2] & flagsMask) | (buf[3] & 0x80) | (buf[4] & 0x80) | (buf[5] & 0x80) | (buf[6] & 0x80)) != 0 || buf[1] == 0xFF))
                        {
                            return size + 10;   // don't forget the sync, flag & size bytes!
                        }
                    }
                    break;
                case 1:
                    return 227 + 128;
                case 0:
                    return 128;
            }

            return -1;
        }

        internal override void Parse()
        {
            // assume we have to process it now or else...  we can still read the whole frame, so no biggie
            switch (_version)
            {
                case 2:
                    ParseV2();
                    break;
                case 1:
                    ParseV1Enh();
                    break;
                case 0:
                    ParseV1(3);
                    break;
            }
        }

        void ParseV1(int offset)
        {
            //var buffer = new byte[125];
            //if (Read(offset, buffer) == 125)
            //{
            //    // v1 tags use ASCII encoding... For now we'll use the built-in encoding, but for Win8 we'll have to build our own.
            //    var encoding = Encoding.ASCII;
            //
            //    // title (30)
            //    Title = encoding.GetString(buffer, 0, 30);
            //
            //    // artist (30)
            //    Artist = encoding.GetString(buffer, 30, 30);
            //
            //    // album (30)
            //    Album = encoding.GetString(buffer, 60, 30);
            //
            //    // year (4)
            //    Year = encoding.GetString(buffer, 90, 30);
            //
            //    // comment (30)*
            //    Comment = encoding.GetString(buffer, 94, 30);
            //
            //    if (buffer[122] == 0)
            //    {
            //        // track (1)*
            //        Track = (int)buffer[123];
            //    }
            //
            //    // genre (1)
            //    // ignore for now
            //
            //    // * if byte 29 of comment is 0, track is byte 30.  Otherwise, track is unknown.
            //}
        }

        void ParseV1Enh()
        {
            ParseV1(230);

            //var buffer = new byte[223];
            //if (Read(4, buffer) == 223)
            //{
            //    // v1 tags use ASCII encoding... For now we'll use the built-in encoding, but for Win8 we'll have to build our own.
            //    var encoding = Encoding.ASCII;
            //
            //    // title (60)
            //    Title += encoding.GetString(buffer, 0, 60);
            //
            //    // artist (60)
            //    Artist += encoding.GetString(buffer, 60, 60);
            //
            //    // album (60)
            //    Album += encoding.GetString(buffer, 120, 60);
            //
            //    // speed (1)
            //    //var speed = buffer[180];
            //
            //    // genre (30)
            //    Genre = encoding.GetString(buffer, 181, 30);
            //
            //    // start-time (6)
            //    // 211
            //
            //    // end-time (6)
            //    // 217
            //}
        }

        void ParseV2()
        {
            // v2 is much more complicated than v1...  don't worry about it for now
            // look for any merged frames, as well
        }

        internal int Version
        {
            get
            {
                if (_version == 0) return 1;
                return _version;
            }
        }

        //internal string Title { get; private set; }
        //internal string Artist { get; private set; }
        //internal string Album { get; private set; }
        //internal string Year { get; private set; }
        //internal string Comment { get; private set; }
        //internal int Track { get; private set; }
        //internal string Genre { get; private set; }
        // speed
        //public TimeSpan StartTime { get; private set; }
        //public TimeSpan EndTime { get; private set; }

        internal void Merge(ID3Frame newFrame)
        {
            // just save off the frame for parsing later
        }
    }
}
