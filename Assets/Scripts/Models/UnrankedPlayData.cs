using System;
using Models;

[Serializable]
public class UnrankedPlayData : IPlayData
{
    public string user;
    public string password;

    public string id;
    public string type;
    public int version;

    public long score;
    public int accuracy;
    public int max_combo;
    public int perfect;
    public int great;
    public int good;
    public int bad;
    public int miss;

    public string chart_checksum;
    public string checksum;
}