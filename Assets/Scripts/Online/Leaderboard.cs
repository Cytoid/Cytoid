using System;
using System.Collections.Generic;

[Serializable]
public class Leaderboard
{
    [Serializable]
    public class Entry
    {
        public int rank;
        public float rating;
        public User owner;
    }
    
    [Serializable]
    public class User
    {
        public string uid;
        public string name;
        public string avatarURL;
    }
}