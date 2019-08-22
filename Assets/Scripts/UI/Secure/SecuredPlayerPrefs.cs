using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecuredPlayerPrefs
{
    private const int Iterations = 555;

    private const string Password = "cytoid4ever";
    private const string Salt = "37nz*G!F_&G^bBWClbVv$FcmCvwlk)Ch";

    private static byte[] saltBytes = Encoding.UTF8.GetBytes(Salt);

    public static float GetFloat(string key, float defaultValue)
    {
        if (!HasKey(key)) return defaultValue;
        return float.TryParse(GetString(key, null), out var result) ? result : defaultValue;
    }

    public static int GetInt(string key, int defaultValue)
    {
        if (!HasKey(key)) return defaultValue;
        return int.TryParse(GetString(key, null), out var result) ? result : defaultValue;
    }

    public static string GetString(string key, string defaultValue)
    {
        return !HasKey(key) ? defaultValue : Decrypt(PlayerPrefs.GetString(Encrypt(key)));
    }

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(Encrypt(key));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
    
    public static void SetFloat(string key, float value)
    {
        var strValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        SetString(key, strValue);
    }
    
    public static void SetInt(string key, int value)
    {
        var strValue = Convert.ToString(value);
        SetString(key, strValue);
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(Encrypt(key), Encrypt(value));
    }
    
    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(Encrypt(key));
    }

    private static string Encrypt(string plainString)
    {
        try
        {
            var des = new DESCryptoServiceProvider();

            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Password, saltBytes, Iterations);

            var key = rfc2898DeriveBytes.GetBytes(8);

            using (var memoryStream = new MemoryStream())
            using (var cryptoStream =
                new CryptoStream(memoryStream, des.CreateEncryptor(key, saltBytes), CryptoStreamMode.Write))
            {
                memoryStream.Write(saltBytes, 0, saltBytes.Length);

                var plainTextBytes = Encoding.UTF8.GetBytes(plainString);

                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("SecuredPlayerPrefs: Could not encrypt. " + e);
            return plainString;
        }
    }

    private static string Decrypt(string encryptedString)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(encryptedString);

            using (var memoryStream = new MemoryStream(cipherBytes))
            {
                var des = new DESCryptoServiceProvider();

                var iv = saltBytes;
                memoryStream.Read(iv, 0, iv.Length);

                var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Password, iv, Iterations);

                var key = rfc2898DeriveBytes.GetBytes(8);

                using (var cryptoStream =
                    new CryptoStream(memoryStream, des.CreateDecryptor(key, iv), CryptoStreamMode.Read))
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    var plainString = streamReader.ReadToEnd();
                    return plainString;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("SecuredPlayerPrefs: Could not decrypt. " + e);
            return encryptedString;
        }
    }
}