using System.Text;
using System.IO;

namespace DragonBones
{
    internal class BinaryDataReader : BinaryReader
    {
        private int i;
        private int readLength;

        internal BinaryDataReader(Stream stream) : base(stream)
        {
        }

        internal BinaryDataReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public virtual void Seek(int offset, SeekOrigin origin = SeekOrigin.Current)
        {
            if (offset == 0)
            {
                return;
            }

            BaseStream.Seek(offset, origin);
        }

        public virtual bool[] ReadBooleans(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            bool[] flagArray = new bool[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                flagArray[this.i] = base.ReadBoolean();
                this.i++;
            }
            return flagArray;
        }

        public virtual byte[] ReadBytes(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            byte[] buffer = new byte[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                buffer[this.i] = base.ReadByte();
                this.i++;
            }
            return buffer;
        }

        public virtual char[] ReadChars(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            char[] chArray = new char[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                chArray[this.i] = base.ReadChar();
                this.i++;
            }
            return chArray;
        }

        public virtual decimal[] ReadDecimals(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            decimal[] numArray = new decimal[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadDecimal();
                this.i++;
            }
            return numArray;
        }

        public virtual double[] ReadDoubles(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            double[] numArray = new double[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadDouble();
                this.i++;
            }
            return numArray;
        }

        public virtual short[] ReadInt16s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            short[] numArray = new short[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadInt16();
                this.i++;
            }
            return numArray;
        }

        public virtual int[] ReadInt32s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            int[] numArray = new int[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadInt32();
                this.i++;
            }
            return numArray;
        }

        public virtual long[] ReadInt64s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            long[] numArray = new long[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadInt64();
                this.i++;
            }
            return numArray;
        }

        public virtual sbyte[] ReadSBytes(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            sbyte[] numArray = new sbyte[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadSByte();
                this.i++;
            }
            return numArray;
        }

        public virtual float[] ReadSingles(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            float[] numArray = new float[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadSingle();
                this.i++;
            }
            return numArray;
        }

        public virtual string[] ReadStrings(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            string[] strArray = new string[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                strArray[this.i] = base.ReadString();
                this.i++;
            }
            return strArray;
        }

        public virtual ushort[] ReadUInt16s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            ushort[] numArray = new ushort[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadUInt16();
                this.i++;
            }
            return numArray;
        }

        public virtual uint[] ReadUInt32s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            uint[] numArray = new uint[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadUInt32();
                this.i++;
            }
            return numArray;
        }

        public virtual ulong[] ReadUInt64s(int offset, int readLength)
        {
            Seek(offset);

            this.readLength = readLength;
            ulong[] numArray = new ulong[this.readLength];
            this.i = 0;
            while (this.i < this.readLength)
            {
                numArray[this.i] = base.ReadUInt64();
                this.i++;
            }
            return numArray;
        }

        private long Length
        {
            get
            {
                return this.BaseStream.Length;
            }
        }
    }

}
