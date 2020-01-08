using Proyecto26;
using UniRx.Async;
using UnityEngine.UI;

public class SignInScreen : Screen
{
    public const string Id = "SignIn";

    public InputField uidInput;
    public InputField passwordInput;
    public TransitionElement closeButton;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        uidInput.text = Context.OnlinePlayer.GetUid();
    }

    public async UniTask SignIn()
    {
        if (uidInput.text == "")
        {
            Toast.Next(Toast.Status.Failure, "Please enter your Cytoid ID.".Localized());
            return;
        }
        if (passwordInput.text == "")
        {
            Toast.Next(Toast.Status.Failure, "Please enter your password.".Localized());
            return;
        }

        var completed = false;

        Context.OnlinePlayer.SetUid(uidInput.text.Trim());
        Context.OnlinePlayer.Authenticate(passwordInput.text)
            .Then(profile =>
            {
                Toast.Next(Toast.Status.Success, "Successfully signed in.");
                ProfileWidget.Instance.SetSignedIn(profile);
                Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopHistoryAndPeek(), ScreenTransition.In, addToHistory: false);
            })
            .HandleRequestErrors()
            .Finally(() => completed = true);

        closeButton.Leave();
        await UniTask.WaitUntil(() => completed);
        closeButton.Enter();
    }
}