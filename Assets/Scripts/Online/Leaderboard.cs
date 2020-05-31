using System;
using System.Collections.Generic;

[Serializable]
public class Leaderboard
{
    [Serializable]
    public class Entry : OnlineUser
    {
        public int rank;
        public float rating;
    }
}