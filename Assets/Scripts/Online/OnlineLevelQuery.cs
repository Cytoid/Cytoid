using System;

[Serializable]
public class OnlineLevelQuery
{
    public string sort = "creation_date";
    public string order = "desc";
    public string category = "all";
    public string time = "all";
    public string search = "";
    public string owner = "";

    public string BuildUri(int limit = -1, int page = 0)
    {
        string uri;
        if (search.IsNullOrEmptyTrimmed() && sort == "relevance")
        {
            sort = "creation_date"; // Reset
        }
        if (!search.IsNullOrEmptyTrimmed() || (sort != "creation_date" && sort != "modification_date" && sort != "duration" && sort != "difficulty"))
        {
            uri = $"{Context.ApiUrl}/search/levels?search={search}&" +
                  $"{(sort != "relevance" ? $"sort={sort}" : "")}&order={order}" +
                  $"&date_start={ConvertTimeSpanToDateStart(time)}&owner={owner}&page={page}";
        }
        else
        {
            uri = $"{Context.ApiUrl}/levels?search={search}&" +
                  $"sort={sort}&order={order}" +
                  $"&date_start={ConvertTimeSpanToDateStart(time)}&owner={owner}&page={page}";
        }
        if (category == "featured")
        {
            uri += "&featured=true";
        }
        if (category == "qualified")
        {
            uri += "&qualified=true";
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