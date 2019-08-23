using Proyecto26;
using UnityEngine;

public class ProfileScreen : Screen
{
    public const string Id = "Profile";

    public override string GetId() => Id;

    public UpperOverlay upperOverlay;
    public ContentTabs contentTabs;
    public TransitionElement character;
    public LeaderboardElement leaderboard;
    public InteractableMonoBehavior signOutButton;

    protected override void Awake()
    {
        base.Awake();
        signOutButton.onPointerClick.AddListener(pointerData =>
        {
            Context.OnlinePlayer.Deauthenticate();
            Context.ScreenManager.ChangeScreen(Context.ScreenManager.GetLastScreenId(), ScreenTransition.In);
            Toast.Next(Toast.Status.Success, "Successfully signed out.");
            ProfileWidget.Instance.SetSignedOut();
            Context.ScreenManager.GetScreen<SignInScreen>().passwordInput.text = "";
        });
        contentTabs.onTabSelect.AddListener((index, tab) =>
        {
            var contentRect = tab.transform.Find("Viewport/Content").transform as RectTransform;
            upperOverlay.contentRect = contentRect;
            
            if (index == 0)
            {
                character.Enter();
            }
            else
            {
                character.Leave(false);
            }
        });
    }

    public override void OnScreenBecameActive()
    {
        character.enterDuration = 1.2f;
        character.enterDelay = 0.4f;
        base.OnScreenBecameActive();
        character.enterDuration = 0.4f;
        character.enterDelay = 0;
        RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
        {
            Uri = Context.Host + "/leaderboard"
        }).Then(data => { leaderboard.SetModel(data); }).Catch(Debug.Log);
    }
}