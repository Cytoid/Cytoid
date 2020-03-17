using System;

[Serializable]
public class OnlineUser
{
    public string id;
    public string uid;
    public string name;
    public string avatarURL;
    public OnlineAvatarImageAsset avatar;
}

[Serializable]
public class OnlineAvatarImageAsset
{
    public string original;
    public string small;
    public string large;
}