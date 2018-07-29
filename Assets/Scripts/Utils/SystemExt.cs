using System;
using Newtonsoft.Json;

public static class SystemExt
{
    /// <summary>
    /// Perform a deep Copy of the object, using Json as a serialisation method. NOTE: Private members are not cloned using this method.
    /// </summary>
    /// <typeparam name="T">The type of object being copied.</typeparam>
    /// <param name="source">The object instance to copy.</param>
    /// <returns>The copied object.</returns>
    public static T Clone<T>(this T source)
    {
        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(source, null))
        {
            return default(T);
        }

        // initialize inner objects individually
        // for example in default constructor some list property initialized with some values,
        // but in 'source' these items are cleaned -
        // without ObjectCreationHandling.Replace default constructor values will be added to result
        var deserializeSettings = new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace};

        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
    }

    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException(string.Format("Argumnent {0} is not an Enum", typeof(T).FullName));

        var arr = (T[]) Enum.GetValues(src.GetType());
        var j = Array.IndexOf(arr, src) + 1;
        return arr.Length == j ? arr[0] : arr[j];
    }
}