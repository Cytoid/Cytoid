using System;

public static class TimeExt
{

    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long Millis()
    {
        return (long) (DateTime.UtcNow - Epoch).TotalMilliseconds;
    }
        
}