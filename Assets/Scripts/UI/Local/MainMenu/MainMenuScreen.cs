public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        if (!Context.OnlinePlayer.IsAuthenticated && !Context.OnlinePlayer.IsAuthenticating && !string.IsNullOrEmpty(Context.OnlinePlayer.GetJwtToken()))
        {
            ProfileWidget.Instance.SetSigningIn();
            Context.OnlinePlayer.AuthenticateWithJwtToken()
                .Then(profile =>
                {
                    Toast.Next(Toast.Status.Success, "Successfully signed in.");
                    ProfileWidget.Instance.SetSignedIn(profile);
                })
                .HandleRequestErrors(error => ProfileWidget.Instance.SetSignedOut());
        }
    }
}