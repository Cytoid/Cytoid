using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeCharts;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class ProfileTab : MonoBehaviour
{
    [GetComponent] public RectTransform rectTransform;
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public ScrollRect scrollRect;
    public TransitionElement transitionElement;
    
    public Avatar avatar;
    public TransitionElement characterTransitionElement;
    public RectTransform changeCharacterButton;
    public Image levelProgressImage;
    public Text uidText;
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

    public FullProfile Profile { get; private set; }
    
    public void Update()
    {
        if (!transitionElement.IsShown) return;

        var dy = characterPaddingReference.GetScreenSpaceRect().min.y - 64;
        dy = Math.Max(0, dy);
        dy /= UnityEngine.Screen.height;
        var canvasHeight = 1920f / UnityEngine.Screen.width * UnityEngine.Screen.height;
        dy *= canvasHeight;
        characterTransitionElement.rectTransform.SetAnchoredY(dy);
        changeCharacterButton.SetAnchoredY(64 + dy);
    }

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
        tierText.text = profile.Tier.name;
        tierGradient.SetGradient(new ColorGradient(profile.Tier.colorPalette.background));
        ratingText.text = $"{"PROFILE_WIDGET_RATING".Get()} {profile.Rating:0.00}";
        levelText.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {profile.Exp.CurrentLevel}";
        expText.text = $"{"PROFILE_WIDGET_EXP".Get()} {profile.Exp.TotalExp}/{profile.Exp.NextLevelExp}";
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
                levelCard.SetModel(new LevelView {DisplayOwner = false, Level = level.ToLevel(LevelType.Community)});
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
        var pos = 0;
        foreach (var data in Profile.TimeSeries.AsEnumerable().Reverse().Take(24)) // 6 months
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
            pos++;
        }
        switch (type)
        {
            case ChartType.AvgRating:
                chart.AxisConfig.VerticalAxisConfig.Bounds.Let(it =>
                {
                    it.MaxAutoValue = false;
                    it.MinAutoValue = false;
                    it.Max = Mathf.CeilToInt((float) Profile.TimeSeries.MaxBy(x => x.CumulativeRating).CumulativeRating);
                    it.Min = (int) Profile.TimeSeries.MinBy(x => x.CumulativeRating).CumulativeRating;
                });
                chart.AxisConfig.VerticalAxisConfig.ValueFormatterConfig.ValueDecimalPlaces = 2;
                break;
            case ChartType.AvgAccuracy:
                chart.AxisConfig.VerticalAxisConfig.Bounds.Let(it =>
                {
                    it.MaxAutoValue = false;
                    it.MinAutoValue = false;
                    it.Max = Mathf.CeilToInt((float) Profile.TimeSeries.MaxBy(x => x.CumulativeAccuracy * 100).CumulativeAccuracy * 100);
                    it.Min = (int) (Profile.TimeSeries.MinBy(x => x.CumulativeAccuracy * 100).CumulativeAccuracy * 100);
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
    
}