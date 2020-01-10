using System;

[Serializable]
public class OnlineLevelQuery
{
    public string sort;
    public string order;
    public string category;
    public string time;
    public string search;

    public string BuildUri(int limit = -1, int page = 0)
    {
        var uri = $"{Context.ApiBaseUrl}/levels?" +
                  $"sort={sort}&order={order}&search={search}" +
                  $"&date_start={ConvertTimeSpanToDateStart(time)}&page={page}";
        if (category == "featured")
        {
            uri += "&featured=true";
        }

        if (limit >= 0)
        {
            uri += "&limit=" + limit;
        }

        return uri;
    }

    public static string ConvertTimeSpanToDateStart(string timeSpan)
    {
        if (timeSpan == null || timeSpan == "all") return null;
        var date = DateTime.UtcNow;
        switch (timeSpan)
        {
            case "week":
                date = date - TimeSpan.FromDays(7);
                break;
            case "month":
                date = date - TimeSpan.FromDays(30);
                break;
            case "halfyear":
                date = date - TimeSpan.FromDays(180);
                break;
            case "year":
                date = date - TimeSpan.FromDays(365);
                break;
        }

        return date.ToString("yyyy-MM-dd");
    }
}