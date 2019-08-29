using System;

[Serializable]
public class Profile
{
    public OnlineUser user;
    public float rating;
    public Exp exp;

    [Serializable]
    public class Exp
    {
        public int currentLevel;
        public float totalExp;
        public float currentLevelExp;
        public float nextLevelExp;
    }
}