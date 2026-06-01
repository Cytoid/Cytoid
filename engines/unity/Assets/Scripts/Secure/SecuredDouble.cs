using System;
using System.Globalization;
using System.Security.Cryptography;

public sealed class SecuredDouble
{
    private const long CheckSalt = unchecked((long) 0x4F1BBCDCB7A56391);

    private long _key;
    private long _stored;
    private long _guard;
    private long _check;

    public double Value
    {
        get
        {
            if (_check != ComputeCheck(_stored, _key, _guard))
            {
                throw new InvalidOperationException("SecuredDouble integrity check failed");
            }

            return BitConverter.Int64BitsToDouble(_stored ^ _key);
        }
        private set
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            _key = BitConverter.ToInt64(bytes, 0);
            _guard = BitConverter.ToInt64(bytes, 8);
            _stored = BitConverter.DoubleToInt64Bits(value) ^ _key;
            _check = ComputeCheck(_stored, _key, _guard);
        }
    }

    public SecuredDouble()
    {
        Value = 0.0;
    }

    public SecuredDouble(double it)
    {
        Value = it;
    }

    public static implicit operator double(SecuredDouble it) => it.Value;

    public static implicit operator SecuredDouble(double it) => new SecuredDouble(it);

    public static implicit operator SecuredDouble(float it) => new SecuredDouble(it);

    public static implicit operator SecuredDouble(int it) => new SecuredDouble(it);

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    private static long ComputeCheck(long stored, long key, long guard)
    {
        unchecked
        {
            var hash = CheckSalt;
            hash = (hash * 397) ^ stored;
            hash = (hash * 397) ^ key;
            hash = (hash * 397) ^ guard;
            return hash;
        }
    }
}
