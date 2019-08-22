using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class ProfileScreen : Screen
{
    public ProfileUpperOverlay upperOverlay;
    public ContentTabs contentTabs;
    public LeaderboardElement leaderboard;
    public ButtonElement signOutButton;
    
    public const string Id = "Profile";

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        signOutButton.onClick = () =>
        {
            Context.OnlinePlayer.Deauthenticate();
            Context.ScreenManager.ChangeScreen(Context.ScreenManager.GetLastScreenId(), ScreenTransition.In);
            Toast.Next(Toast.Status.Success, "Successfully signed out.");
            ProfileWidget.Instance.SetSignedOut();
            Context.ScreenManager.GetScreen<SignInScreen>().passwordInput.text = "";
        };
        contentTabs.onTabSelect.AddListener((index, tab) =>
        {
            print(tab.name);
            var contentRect = tab.transform.Find("Viewport/Content").transform as RectTransform;
            upperOverlay.contentRect = contentRect;
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
        {
            Uri = Context.Host + "/leaderboard"
        }).Then(data => { leaderboard.SetModel(data); }).Catch(Debug.Log);
    }
}