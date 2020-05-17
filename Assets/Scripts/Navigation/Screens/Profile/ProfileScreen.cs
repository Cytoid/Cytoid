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
    public InteractableMonoBehavior toggleOfflineButton;

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
            Application.OpenURL(Context.WebsiteUrl + "/profile/" + Context.OnlinePlayer.LastProfile.User.Uid));
        signOutButton.onPointerClick.AddListener(_ =>
        {
            Context.OnlinePlayer.Deauthenticate();
            Context.SetOffline(false);
            Context.ScreenManager.History.Clear();
            Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_SIGNED_OUT".Get());
            ProfileWidget.Instance.SetSignedOut();
            Context.ScreenManager.GetScreen<SignInScreen>().passwordInput.text = "";
        });
        contentTabs.onTabSelect.AddListener((index, tab) =>
        {
            upperOverlay.contentRect = contentTabs.viewportContents[index];

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
                if (Context.IsOnline()) UpdateLeaderboard(leaderboardModeSelect.Value);
            }
        });
        leaderboardModeSelect.onSelect.AddListener(UpdateLeaderboard);
        leaderboardScrollRect.gameObject.AddComponent<ScrollRectFocusHelper>();
        toggleOfflineButton.onPointerClick.AddListener(_ => ToggleOffline());
    }

    public override void OnScreenBecameActive()
    {
        characterTransitionElement.enterDuration = 1.2f;
        characterTransitionElement.enterDelay = 0.4f;
        base.OnScreenBecameActive();
        
        characterTransitionElement.onEnterStarted.SetListener(() =>
        {
            characterTransitionElement.enterDuration = 0.4f;
            characterTransitionElement.enterDelay = 0;
        });

        var profile = Context.OnlinePlayer.LastProfile;
        avatarImage.sprite = ProfileWidget.Instance.avatarImage.sprite;
        levelProgressImage.fillAmount = (profile.Exp.TotalExp - profile.Exp.CurrentLevelExp)
                                        / (profile.Exp.NextLevelExp - profile.Exp.CurrentLevelExp);
        uidText.text = profile.User.Uid;
        ratingText.text = $"{"PROFILE_WIDGET_RATING".Get()} {profile.Rating:0.00}";
        levelText.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {profile.Exp.CurrentLevel}";
        expText.text = $"{"PROFILE_WIDGET_EXP".Get()} {profile.Exp.TotalExp}/{profile.Exp.NextLevelExp}";
        totalRankedPlaysText.text = profile.Activities.TotalRankedPlays.ToString("N0");
        totalClearedNotesText.text = profile.Activities.ClearedNotes.ToString("N0");
        highestMaxComboText.text = profile.Activities.MaxCombo.ToString("N0");
        avgRankedAccuracyText.text = ((profile.Activities.AverageRankedAccuracy ?? 0) * 100).ToString("0.00") + "%";
        totalRankedScoreText.text = profile.Activities.TotalRankedScore.ToString("N0");
        totalPlayTimeText.text = TimeSpan.FromSeconds(profile.Activities.TotalPlayTime)
            .Let(it => it.ToString(it.Days > 0 ? @"d\d\ h\h\ m\m\ s\s" : @"h\h\ m\m\ s\s"));
        LayoutFixer.Fix(ratingText.transform.parent.transform);
        
        toggleOfflineButton.GetComponentInChildren<Text>().text = Context.IsOnline() ? "OFFLINE_GO_OFFLINE".Get() : "OFFLINE_GO_ONLINE".Get();
    }

    public void ToggleOffline()
    {
        Context.ScreenManager.History.Clear();
        Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.Out);
        if (Context.IsOnline())
        {
            Context.SetOffline(true);
            Toast.Enqueue(Toast.Status.Success, "TOAST_SWITCHED_TO_OFFLINE_MODE".Get());
        }
        else
        {
            ProfileWidget.Instance.SetSigningIn();
        }
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
                var meEntry = leaderboard.Entries.Find(it => it.Model.owner.Uid == Context.OnlinePlayer.Uid);
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