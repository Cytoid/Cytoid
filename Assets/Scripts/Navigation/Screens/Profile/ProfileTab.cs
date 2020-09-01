using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AwesomeCharts;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ProfileTab : MonoBehaviour
{
    
    [GetComponent] public RectTransform rectTransform;
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public ScrollRect scrollRect;
    
    public Avatar avatar;
    public CharacterDisplay characterDisplay;
    public TransitionElement characterTransitionElement;
    public RectTransform changeCharacterButton;
    public Image levelProgressImage;
    public Text uidText;
    public Image statusCircleImage;
    public Text statusText;
    public Text tierText;
    public GradientMeshEffect tierGradient;
    public Text ratingText;
    public Text levelText;
    public Text expText;
    public Text totalRankedPlaysText;
    public Text totalClearedNotesText;
    public Text highestMaxComboText;
    public Text avgRankedAccuracyText;
    public Text totalRankedScoreText;
    public Text totalPlayTimeText;
    public RadioGroup chartRadioGroup;
    public LineChart chart;
    public RecordSection recordSection;
    public RecordCard recordCardPrefab;
    public LevelSection levelSection;
    public InteractableMonoBehavior viewAllLevelsButton;
    public InteractableMonoBehavior viewAllFeaturedLevelsButton;
    public LevelCard levelCardPrefab;
    public CollectionSection collectionSection;
    public CollectionCard collectionCardPrefab;

    public RectTransform characterPaddingReference;
    
    public List<Transform> pillRows;
    public Transform sectionParent;

    public BadgeGrid badgeGrid;

    public FullProfile Profile { get; private set; }

    public async void SetModel(FullProfile profile)
    {
        Profile = profile;
        characterTransitionElement.Leave(false, true);
        characterTransitionElement.enterDuration = 1.2f;
        characterTransitionElement.enterDelay = 0.4f;
        characterTransitionElement.onEnterStarted.SetListener(() =>
        {
            characterTransitionElement.enterDuration = 0.4f;
            characterTransitionElement.enterDelay = 0;
        });

        avatar.SetModel(profile.User);
        levelProgressImage.fillAmount = (profile.Exp.TotalExp - profile.Exp.CurrentLevelExp)
                                        / (profile.Exp.NextLevelExp - profile.Exp.CurrentLevelExp);
        uidText.text = profile.User.Uid;

        void MarkOffline()
        {
            statusCircleImage.color = "#757575".ToColor();
            statusText.text = "PROFILE_STATUS_OFFLINE".Get();
        }
        void MarkOnline()
        {
            statusCircleImage.color = "#47dc47".ToColor();
            statusText.text = "PROFILE_STATUS_ONLINE".Get();
        }
        if (profile.User.Uid == Context.OnlinePlayer.LastProfile?.User.Uid)
        {
            if (Context.IsOffline())
            {
                MarkOffline();
            }
            else
            {
                MarkOnline();
            }
        }
        else
        {
            if (profile.LastActive == null)
            {
                MarkOffline();
            }
            else
            {
                var lastActive = profile.LastActive.Value.LocalDateTime;
                if (DateTime.Now - lastActive <= TimeSpan.FromMinutes(30))
                {
                    MarkOnline();
                }
                else
                {
                    statusCircleImage.color = "#757575".ToColor();
                    statusText.text = "PROFILE_STATUS_LAST_SEEN_X".Get(lastActive.Humanize());
                }
            }
        }

        if (profile.Tier == null)
        {
            tierText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            tierText.transform.parent.gameObject.SetActive(true);
            tierText.text = profile.Tier.name;
            tierGradient.SetGradient(new ColorGradient(profile.Tier.colorPalette.background));
        }
        ratingText.text = $"{"PROFILE_WIDGET_RATING".Get()} {profile.Rating:0.00}";
        levelText.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {profile.Exp.CurrentLevel}";
        expText.text = $"{"PROFILE_WIDGET_EXP".Get()} {(int) profile.Exp.TotalExp}/{(int) profile.Exp.NextLevelExp}";
        totalRankedPlaysText.text = profile.Activities.TotalRankedPlays.ToString("N0");
        totalClearedNotesText.text = profile.Activities.ClearedNotes.ToString("N0");
        highestMaxComboText.text = profile.Activities.MaxCombo.ToString("N0");
        avgRankedAccuracyText.text = ((profile.Activities.AverageRankedAccuracy ?? 0) * 100).ToString("0.00") + "%";
        totalRankedScoreText.text = (profile.Activities.TotalRankedScore ?? 0).ToString("N0");
        totalPlayTimeText.text = TimeSpan.FromSeconds(profile.Activities.TotalPlayTime)
            .Let(it => it.ToString(it.Days > 0 ? @"d\d\ h\h\ m\m\ s\s" : @"h\h\ m\m\ s\s"));
        
        chartRadioGroup.onSelect.SetListener(type => UpdateChart((ChartType) Enum.Parse(typeof(ChartType), type, true)));
        UpdateChart(ChartType.AvgRating);
        
        pillRows.ForEach(it => LayoutFixer.Fix(it));
        var eventBadges = profile.GetEventBadges();
        if (eventBadges.Any())
        {
            badgeGrid.gameObject.SetActive(true);
            badgeGrid.SetModel(eventBadges);
        }
        else
        {
            badgeGrid.gameObject.SetActive(false);
        }

        foreach (Transform child in recordSection.recordCardHolder) Destroy(child.gameObject);
        foreach (Transform child in levelSection.levelCardHolder) Destroy(child.gameObject);
        foreach (Transform child in collectionSection.collectionCardHolder) Destroy(child.gameObject);

        if (profile.RecentRecords.Count > 0)
        {
            recordSection.gameObject.SetActive(true);
            foreach (var record in profile.RecentRecords.Take(6))
            {
                var recordCard = Instantiate(recordCardPrefab, recordSection.recordCardHolder);
                recordCard.SetModel(new RecordView{DisplayOwner = false, Record = record});
            }
        }
        else
        {
            recordSection.gameObject.SetActive(false);
        }

        if (profile.LevelCount > 0)
        {
            levelSection.gameObject.SetActive(true);
            foreach (var level in profile.Levels.Take(6))
            {
                var levelCard = Instantiate(levelCardPrefab, levelSection.levelCardHolder);
                levelCard.SetModel(new LevelView {DisplayOwner = false, Level = level.ToLevel(LevelType.User)});
            }

            viewAllLevelsButton.GetComponentInChildren<Text>().text = "PROFILE_VIEW_ALL_X".Get(profile.LevelCount);
            viewAllLevelsButton.onPointerClick.SetListener(_ =>
            {
                Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In, 0.4f,
                    transitionFocus: ((RectTransform) viewAllLevelsButton.transform).GetScreenSpaceCenter(),
                    payload: new CommunityLevelSelectionScreen.Payload
                        {
                            Query = new OnlineLevelQuery{owner = profile.User.Uid, category = "all", sort = "creation_date", order = "desc"},
                        });
            });
            if (profile.FeaturedLevelCount > 0)
            {
                viewAllFeaturedLevelsButton.gameObject.SetActive(true);
                viewAllFeaturedLevelsButton.GetComponentInChildren<Text>().text =
                    "PROFILE_VIEW_FEATURED_X".Get(profile.FeaturedLevelCount);
                viewAllFeaturedLevelsButton.onPointerClick.SetListener(_ =>
                {
                    Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In, 0.4f,
                        transitionFocus: ((RectTransform) viewAllFeaturedLevelsButton.transform).GetScreenSpaceCenter(),
                        payload: new CommunityLevelSelectionScreen.Payload
                        {
                            Query = new OnlineLevelQuery{owner = profile.User.Uid, category = "featured", sort = "creation_date", order = "desc"},
                        });
                });
            }
            else
            {
                viewAllFeaturedLevelsButton.gameObject.SetActive(false);
            }
            
            viewAllLevelsButton.transform.parent.RebuildLayout();
        }
        else
        {
            levelSection.gameObject.SetActive(false);
        }

        if (profile.CollectionCount > 0)
        {
            collectionSection.gameObject.SetActive(true);
            foreach (var collection in profile.Collections.Take(6))
            {
                var collectionCard = Instantiate(collectionCardPrefab, collectionSection.collectionCardHolder);
                collectionCard.SetModel(collection);
            }
        }
        else
        {
            collectionSection.gameObject.SetActive(false);
        }

        LayoutFixer.Fix(sectionParent);

        await UniTask.DelayFrame(5);
        
        transform.RebuildLayout();
        
        await UniTask.DelayFrame(0);
        
        characterTransitionElement.Enter();
    }

    private void UpdateChart(ChartType type)
    {
        var entries = new List<LineEntry>();
        var dates = new List<string>();
        var pos = 0;
        var allData = Profile.TimeSeries.AsEnumerable().Reverse().Take(24).Reverse().ToList();
        foreach (var data in allData) // 6 months
        {
            var value = 0.0;
            switch (type)
            {
                case ChartType.AvgRating:
                    value = data.CumulativeRating;
                    break;
                case ChartType.AvgAccuracy:
                    value = data.CumulativeAccuracy * 100;
                    break;
                case ChartType.Plays:
                    value = data.Count;
                    break;
            }
            entries.Add(new LineEntry(pos, (float) value));
            var date = FirstDateOfWeekISO8601(int.Parse(data.Year), int.Parse(data.Week));
            if (date.Day >= 1 && date.Day <= 7)
            {
                dates.Add(date.ToString("MMM"));
            }
            else
            {
                dates.Add("");
            }
            pos++;
        }

        chart.AxisConfig.HorizontalAxisConfig.LabelsCount = entries.Count;
        chart.AxisConfig.HorizontalAxisConfig.ValueFormatterConfig.CustomValues = dates;
        switch (type)
        {
            case ChartType.AvgRating:
                chart.AxisConfig.VerticalAxisConfig.Bounds.Let(it =>
                {
                    it.MaxAutoValue = false;
                    it.MinAutoValue = false;
                    it.Max = allData.Count == 0 ? 16 : Mathf.CeilToInt((float) allData.MaxBy(x => x.CumulativeRating).CumulativeRating);
                    it.Min = allData.Count == 0 ? 0 : (int) allData.MinBy(x => x.CumulativeRating).CumulativeRating;
                });
                chart.AxisConfig.VerticalAxisConfig.ValueFormatterConfig.ValueDecimalPlaces = 2;
                break;
            case ChartType.AvgAccuracy:
                chart.AxisConfig.VerticalAxisConfig.Bounds.Let(it =>
                {
                    it.MaxAutoValue = false;
                    it.MinAutoValue = false;
                    it.Max = allData.Count == 0 ? 100 : Mathf.CeilToInt((float) allData.MaxBy(x => x.CumulativeAccuracy).CumulativeAccuracy * 100);
                    it.Min = allData.Count == 0 ? 0 : (int) (allData.MinBy(x => x.CumulativeAccuracy).CumulativeAccuracy * 100);
                });
                chart.AxisConfig.VerticalAxisConfig.ValueFormatterConfig.ValueDecimalPlaces = 2;
                break;
            case ChartType.Plays:
                chart.AxisConfig.VerticalAxisConfig.Bounds.Let(it =>
                {
                    it.MaxAutoValue = true;
                    it.MinAutoValue = true;
                });
                chart.AxisConfig.VerticalAxisConfig.ValueFormatterConfig.ValueDecimalPlaces = 0;
                break;
        }
        chart.GetChartData().DataSets.First().Entries = entries;
        chart.SetDirty();
    }

    private enum ChartType
    {
        AvgRating, AvgAccuracy, Plays
    }
    
    /**
     * Credits:
     * https://stackoverflow.com/a/9064954/2706176
     */
    private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

        // Use first Thursday in January to get first week of the year as
        // it will never be in Week 52/53
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        var weekNum = weekOfYear;
        // As we're adding days to a date in Week 1,
        // we need to subtract 1 in order to get the right date for week #1
        if (firstWeek == 1)
        {
            weekNum -= 1;
        }

        // Using the first Thursday as starting week ensures that we are starting in the right year
        // then we add number of weeks multiplied with days
        var result = firstThursday.AddDays(weekNum * 7);

        // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
        return result.AddDays(-3);
    }       
    
}