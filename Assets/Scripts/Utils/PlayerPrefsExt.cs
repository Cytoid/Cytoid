using UnityEngine;
using UnityEngine.UI;

public static class PlayerPrefsExt
{
    public static void SetBool(string name, bool value)
    {
        PlayerPrefs.SetInt(name, value ? 1 : 0);
    }

    public static bool GetBool(string name)
    {
        return PlayerPrefs.GetInt(name) == 1;
    }

    public static bool GetBool(string name, bool defaultValue)
    {
        return PlayerPrefs.HasKey(name) ? GetBool(name) : defaultValue;
    }
}