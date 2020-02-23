using System.Text;
using System.IO;

namespace DragonBones
{
    internal class BinaryDataWriter : BinaryWriter
    {
        public BinaryDataWriter() : base(new MemoryStream(0x100))
        {
        }

        internal BinaryDataWriter(Stream stream) : base(stream)
        {
        }

        public BinaryDataWriter(Encoding encoding) : base(new MemoryStream(0x100), encoding)
        {
        }

        internal BinaryDataWriter(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public virtual void Write(bool[] value)
        {
            foreach (bool flag in value)
            {
                base.Write(flag);
            }
        }

        public override void Write(byte[] value)
        {
            foreach (byte num in value)
            {
                base.Write(num);
            }
        }

        public override void Write(char[] value)
        {
            foreach (char ch in value)
            {
                base.Write(ch);
            }
        }

        public virtual void Write(decimal[] value)
        {
            foreach (decimal num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(double[] value)
        {
            foreach (double num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(short[] value)
        {
            foreach (short num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(int[] value)
        {
            foreach (int num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(long[] value)
        {
            foreach (long num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(sbyte[] value)
        {
            foreach (sbyte num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(float[] value)
        {
            foreach (float num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(string[] value)
        {
            foreach (string str in value)
            {
                base.Write(str);
            }
        }

        public virtual void Write(ushort[] value)
        {
            foreach (ushort num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(uint[] value)
        {
            foreach (uint num in value)
            {
                base.Write(num);
            }
        }

        public virtual void Write(ulong[] value)
        {
            foreach (ulong num in value)
            {
                base.Write(num);
            }
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
