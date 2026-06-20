using System;
using System.Globalization;
using System.Security.Cryptography;

public sealed class SecuredInt
{
    private const int CheckSalt = unchecked((int) 0x6C8E9CF5);

    private int _key;
    private int _stored;
    private int _guard;
    private int _check;

    public int Value
    {
        get
        {
            if (_check != ComputeCheck(_stored, _key, _guard))
            {
                throw new InvalidOperationException("SecuredInt integrity check failed");
            }

            return _stored ^ _key;
        }
        private set
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            _key = BitConverter.ToInt32(bytes, 0);
            _guard = BitConverter.ToInt32(bytes, 4);
            _stored = value ^ _key;
            _check = ComputeCheck(_stored, _key, _guard);
        }
    }

    public SecuredInt()
    {
        Value = 0;
    }

    public SecuredInt(int it)
    {
        Value = it;
    }

    public static implicit operator int(SecuredInt it) => it.Value;

    public static implicit operator SecuredInt(int it) => new SecuredInt(it);

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    private static int ComputeCheck(int stored, int key, int guard)
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
