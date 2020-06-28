using System.Globalization;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class SignInScreen : Screen
{
    public const string Id = "SignIn";

    public InputField uidInput;
    public InputField passwordInput;
    public TransitionElement closeButton;
    public CharacterDisplay characterDisplay;
    public InteractableMonoBehavior signUpButton;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        uidInput.text = Context.Player.Id;
        signUpButton.onPointerClick.SetListener(_ =>
        {
            if (Context.Distribution == Distribution.China)
            {
                // TODO: Remove this
                Dialog.PromptAlert("<b>提示：</b>\n如果注册中遇到任何问题，请查看论坛置顶的故障合集贴。", 
                    () => Application.OpenURL($"{Context.WebsiteUrl}/session/signup"));
            }
            else
            {
                Application.OpenURL($"{Context.WebsiteUrl}/session/signup");
            }
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        characterDisplay.Load(CharacterAsset.GetTachieBundleId("Sayaka"));
    }

    public async UniTask SignIn()
    {
        if (uidInput.text == "")
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_ID".Get());
            return;
        }
        if (passwordInput.text == "")
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_PASSWORD".Get());
            return;
        }

        uidInput.text = uidInput.text.ToLower(CultureInfo.InvariantCulture);

        var completed = false;

        Context.Player.Settings.PlayerId = uidInput.text.Trim();
        Context.Player.SaveSettings();
        Context.OnlinePlayer.Authenticate(passwordInput.text)
            .Then(profile =>
            {
                if (profile == null)
                {
                    Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
                    return;
                }
                Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_SIGNED_IN".Get());
                ProfileWidget.Instance.SetSignedIn(profile);
                Context.AudioManager.Get("ActionSuccess").Play();
                Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.In, addTargetScreenToHistory: false);
            })
            .CatchRequestError(error =>
            {
                if (error.IsNetworkError)
                {
                    Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
                }
                else
                {
                    switch (error.StatusCode)
                    {
                        case 401:
                            Toast.Next(Toast.Status.Failure, "TOAST_INCORRECT_ID_OR_PASSWORD".Get());
                            break;
                        case 403:
                            Toast.Next(Toast.Status.Failure, "TOAST_LIKELY_INCORRECT_SYSTEM_TIME".Get());
                            break;
                        case 404:
                            Toast.Next(Toast.Status.Failure, "TOAST_ID_NOT_FOUND".Get());
                            break;
                        default:
                            Toast.Next(Toast.Status.Failure, "TOAST_STATUS_CODE".Get(error.StatusCode));
                            break;
                    }
                }
            })
            .Finally(() => completed = true);

        closeButton.Leave();
        await UniTask.WaitUntil(() => completed);
        closeButton.Enter();
    }
}