using System;

[Serializable]
public class Profile
{
    public User user;
    public float rating;
    public Exp exp;

    [Serializable]
    public class User
    {
        public string uid;
        public string name;
        public string avatarURL;
    }

    [Serializable]
    public class Exp
    {
        public int currentLevel;
        public float totalExp;
        public float currentLevelExp;
        public float nextLevelExp;
    }
    
}