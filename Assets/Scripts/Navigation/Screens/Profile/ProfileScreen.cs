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
    public LeaderboardContainer leaderboard;
    public RadioGroup leaderboardModeSelect;
    public ScrollRect leaderboardScrollRect;
    public InteractableMonoBehavior signOutButton;
    public InteractableMonoBehavior toggleOfflineButton;

    public ProfileTab profileTab;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
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
                profileTab.transitionElement.Enter();
            }
            else
            {
                profileTab.transitionElement.Leave(false);
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
        base.OnScreenBecameActive();
        SpinnerOverlay.Show();
        profileTab.transitionElement.Leave(false, true);
        RestClient.Get<FullProfile>(new RequestHelper
        {
            Uri = "http://localhost:3000", //$"{Context.ApiUrl}/profile/{Context.Player.Id}/details",
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(data =>
        {
            profileTab.SetModel(data);
            profileTab.transitionElement.Enter();
        }).CatchRequestError(error =>
        {
            Debug.LogError(error);
            Dialog.PromptGoBack("Fuck.");
        }).Finally(() => SpinnerOverlay.Hide());
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
            Context.SetOffline(false);
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
        if (mode == "me") uri += "&user=" + Context.Player.Id;
        RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
        {
            Uri = uri,
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(async data =>
        {
            try
            {
                leaderboard.SetData(data);
                if (mode == "me")
                {
                    var meEntry = leaderboard.Entries.Find(it => it.Model.Uid == Context.Player.Id);
                    if (meEntry != null)
                    {
                        await UniTask.DelayFrame(0);
                        leaderboardScrollRect.GetComponent<ScrollRectFocusHelper>()
                            .CenterOnItem(meEntry.transform as RectTransform);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }).CatchRequestError(Debug.LogError).Finally(() => SpinnerOverlay.Hide());
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