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
        public OnlineUser owner;
    }
}