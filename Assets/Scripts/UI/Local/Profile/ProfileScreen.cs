using Proyecto26;
using UnityEngine;

public class ProfileScreen : Screen
{
    public const string Id = "Profile";

    public override string GetId() => Id;

    public UpperOverlay upperOverlay;
    public ContentTabs contentTabs;
    public InteractableMonoBehavior playerAvatar;
    public TransitionElement character;
    public LeaderboardContainer leaderboard;
    public InteractableMonoBehavior signOutButton;

    private bool populatedLeaderboard;

    protected override void Awake()
    {
        base.Awake();
        playerAvatar.onPointerClick.AddListener(_ => Application.OpenURL(Context.WebsiteUrl + "/profile/" + Context.OnlinePlayer.LastProfile.user.uid));
        signOutButton.onPointerClick.AddListener(_ =>
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

            if (index == 1)
            {
                if (!populatedLeaderboard)
                {
                    populatedLeaderboard = true;
                    leaderboard.Clear();
                    RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
                    {
                        Uri = Context.ApiBaseUrl + "/leaderboard"
                    }).Then(data => { leaderboard.SetData(data); }).Catch(Debug.Log);
                }
            }
        });
    }

    public override void OnScreenBecameActive()
    {
        populatedLeaderboard = false;
        character.enterDuration = 1.2f;
        character.enterDelay = 0.4f;
        base.OnScreenBecameActive();
        character.enterDuration = 0.4f;
        character.enterDelay = 0;
    }
}