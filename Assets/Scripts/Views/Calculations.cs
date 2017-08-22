public static class Calculations {

    public static float Score(Chart chart, PlayData playData)
    {
        var score = 0f;
        var combo = 0;
        var allPerfect = true; // or excellent
        var total = chart.notes.Count;
        for (var i = 0; i < chart.chronologicalIds.Count; i++)
        {
            var rank = playData.NoteRankings[i];
            if (rank == NoteRanking.Undetermined) continue;
            if (rank == NoteRanking.Bad || rank == NoteRanking.Miss) combo = 0;
            else combo++;
            if (rank != NoteRanking.Perfect && rank != NoteRanking.Excellent) allPerfect = false;
            score += 900000f / total * rank.ScoreWeight() + 100000f / (total * (float) (total + 1) / 2f) * combo;
        }
        if (allPerfect && playData.NoteCleared == total) score = 1000000;
        return score;
    }
    
    public static float Tp(Chart chart, PlayData playData)
    {
        if (playData.NoteCleared == 0) return 100;
        var tp = 0f;
        var total = playData.NoteCleared;
        for (var i = 0; i < chart.chronologicalIds.Count; i++)
        {
            var rank = playData.NoteRankings[i];
            if (rank == NoteRanking.Undetermined) continue;
            tp += 100f / total * rank.TpWeight();
        }
        return tp;
    }
    
    private static float FullTp(Chart chart, PlayData playData)
    {
        if (playData.NoteCleared == 0) return 100;
        var tp = 0f;
        var total = chart.notes.Count;
        for (var i = 0; i < chart.chronologicalIds.Count; i++)
        {
            if (!playData.NoteRankings.ContainsKey(i))
            {
                continue;
            }
            var rank = playData.NoteRankings[i];
            tp += 100f / total * rank.TpWeight();
        }
        return tp;
    }
    
}