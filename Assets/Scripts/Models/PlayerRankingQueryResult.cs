public class PlayerRankingQueryResult
{
    public int status;
    public int player_rank;
    public Ranking[] rankings = { };

    public class Ranking
    {
        public string player;
        public int rank;
        public string data;
        public string avatar_url;
    }
}