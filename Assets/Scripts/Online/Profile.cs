using System;
using Newtonsoft.Json;

[Serializable]
public class Profile
{
    public OnlineUser user;
    public float rating;
    public Exp exp;
    public Grade grade;
    public Activities activities;
    
    [Serializable]
    public class Exp
    {
        public int currentLevel;
        public float totalExp;
        public float currentLevelExp;
        public float nextLevelExp;
    }

    [Serializable]
    public class Grade
    {
        public int MAX;
        public int SSS;
        public int SS;
        public int S;
        public int A;
        public int B;
        public int C;
        public int D;
        public int F;
    }

    [Serializable]
    public class Activities
    {
        public int total_ranked_plays;
        public long cleared_notes;
        public int max_combo;
        public double average_ranked_accuracy;
        public long total_ranked_score;
        public long total_play_time;
    }
    
}