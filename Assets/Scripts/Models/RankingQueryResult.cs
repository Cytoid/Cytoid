public class RankingQueryResult
{
    public int status;
    public int player_rank;
    public Ranking[] rankings = { };

    public class Ranking
    {
        public int rank;
        public string player;
        public int score;
        public int accuracy;
        public string avatar_url;
    }
}