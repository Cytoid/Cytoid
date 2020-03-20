using System;
using System.Linq.Expressions;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class ProfileScreen : Screen, ScreenChangeListener
{
    public const string Id = "Profile";

    public override string GetId() => Id;

    public UpperOverlay upperOverlay;
    public ContentTabs contentTabs;
    public InteractableMonoBehavior playerAvatar;
    public TransitionElement characterTransitionElement;
    public LeaderboardContainer leaderboard;
    public RadioGroup leaderboardModeSelect;
    public ScrollRect leaderboardScrollRect;
    public InteractableMonoBehavior signOutButton;

    public Image avatarImage;
    public Image levelProgressImage;
    public Text uidText;
    public Text ratingText;
    public Text levelText;
    public Text expText;
    public Text totalRankedPlaysText;
    public Text totalClearedNotesText;
    public Text highestMaxComboText;
    public Text avgRankedAccuracyText;
    public Text totalRankedScoreText;
    public Text totalPlayTimeText;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        playerAvatar.onPointerClick.AddListener(_ =>
            Application.OpenURL(Context.WebsiteUrl + "/profile/" + Context.OnlinePlayer.LastProfile.user.uid));
        signOutButton.onPointerClick.AddListener(_ =>
        {
            Context.OnlinePlayer.Deauthenticate();
            Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.In,
                addToHistory: false);
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_SIGNED_OUT".Get());
            ProfileWidget.Instance.SetSignedOut();
            Context.ScreenManager.GetScreen<SignInScreen>().passwordInput.text = "";
        });
        contentTabs.onTabSelect.AddListener((index, tab) =>
        {
            var contentRect = tab.transform.Find("Viewport/Content").transform as RectTransform;
            upperOverlay.contentRect = contentRect;

            if (index == 0)
            {
                characterTransitionElement.Enter();
            }
            else
            {
                characterTransitionElement.Leave(false);
            }

            if (index == 1)
            {
                UpdateLeaderboard(leaderboardModeSelect.Value);
            }
        });
        leaderboardModeSelect.onSelect.AddListener(UpdateLeaderboard);
        leaderboardScrollRect.gameObject.AddComponent<ScrollRectFocusHelper>();
    }

    public override void OnScreenBecameActive()
    {
        characterTransitionElement.enterDuration = 1.2f;
        characterTransitionElement.enterDelay = 0.4f;
        base.OnScreenBecameActive();
        
        characterTransitionElement.onEnterStarted.RemoveAllListeners();
        characterTransitionElement.onEnterStarted.AddListener(() =>
        {
            characterTransitionElement.enterDuration = 0.4f;
            characterTransitionElement.enterDelay = 0;
        });

        var profile = Context.OnlinePlayer.LastProfile;
        avatarImage.sprite = ProfileWidget.Instance.avatarImage.sprite;
        levelProgressImage.fillAmount = (profile.exp.totalExp - profile.exp.currentLevelExp)
                                        / (profile.exp.nextLevelExp - profile.exp.currentLevelExp);
        uidText.text = profile.user.uid;
        ratingText.text = $"{"PROFILE_WIDGET_RATING".Get()} {profile.rating:0.00}";
        levelText.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {profile.exp.currentLevel}";
        expText.text = $"{"PROFILE_WIDGET_EXP".Get()} {profile.exp.totalExp}/{profile.exp.nextLevelExp}";
        totalRankedPlaysText.text = profile.activities.total_ranked_plays.ToString("N0");
        totalClearedNotesText.text = profile.activities.cleared_notes.ToString("N0");
        highestMaxComboText.text = profile.activities.max_combo.ToString("N0");
        avgRankedAccuracyText.text = (profile.activities.average_ranked_accuracy * 100).ToString("0.00") + "%";
        totalRankedScoreText.text = profile.activities.total_ranked_score.ToString("N0");
        totalPlayTimeText.text = TimeSpan.FromSeconds(profile.activities.total_play_time)
            .Let(it => it.ToString(it.Days > 0 ? @"d\d\ h\h\ m\m\ s\s" : @"h\h\ m\m\ s\s"));
        LayoutFixer.Fix(ratingText.transform.parent.transform);
    }

    public void ClearLeaderboard()
    {
        leaderboardScrollRect.normalizedPosition = new Vector2(0, 1);
        leaderboard.Clear();
    }

    public void UpdateLeaderboard(string mode)
    {
        ClearLeaderboard();

        SpinnerOverlay.Show();
        
        var uri = Context.ApiUrl + "/leaderboard?limit=50";
        if (mode == "me") uri += "&user=" + Context.OnlinePlayer.Uid;
        RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
        {
            Uri = uri,
            Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
        }).Then(async data =>
        {
            leaderboard.SetData(data);
            if (mode == "me")
            {
                var meEntry = leaderboard.Entries.Find(it => it.Model.owner.uid == Context.OnlinePlayer.Uid);
                if (meEntry != null)
                {
                    await UniTask.DelayFrame(0);
                    leaderboardScrollRect.GetComponent<ScrollRectFocusHelper>()
                        .CenterOnItem(meEntry.transform as RectTransform);
                }
            }
        }).Catch(Debug.Log).Finally(() => SpinnerOverlay.Hide());
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            ClearLeaderboard();
        }
    }
}