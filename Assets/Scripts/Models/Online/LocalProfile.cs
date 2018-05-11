using UnityEngine;

public class LocalProfile
{

    public string username;
    public string password;
    public string avatarUrl;
    
    public Texture2D avatarTexture;
    
    public int localVersion;
    
    private LocalProfile()
    {
        localVersion = PlayerPrefs.HasKey(Keys.LocalVersion) ? PlayerPrefs.GetInt(Keys.LocalVersion) : 0;
    }

    public void Load()
    {
        username = PlayerPrefs.GetString(Keys.Username);
        password = PlayerPrefs.GetString(Keys.Password);
        avatarUrl = PlayerPrefs.GetString(Keys.AvatarUrl);
    }

    public void Save()
    {
        PlayerPrefs.SetString(Keys.Username, username);
        PlayerPrefs.SetString(Keys.Password, password);
        PlayerPrefs.SetString(Keys.AvatarUrl, avatarUrl);
        PlayerPrefs.SetInt(Keys.LocalVersion, localVersion);
    }

    public static LocalProfile Init(string username, string password, string avatarUrl)
    {
        var profile = new LocalProfile
        {
            username = username,
            password = password,
            avatarUrl = avatarUrl
        };
        profile.Save();
        Instance = profile;
        return profile;
    }

    public static void reset()
    {
        Instance = null;
    }

    public static LocalProfile Instance;

    public static bool Exists()
    {
        return Instance != null;
    }

    public class Keys
    {
        public const string Username = "profile_username";
        public const string Password = "profile_password";
        public const string AvatarUrl = "profile_avatar_url";
        public const string LocalVersion = "profile_local_version";
    }

}