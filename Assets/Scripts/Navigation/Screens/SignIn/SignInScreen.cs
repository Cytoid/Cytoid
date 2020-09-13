using System;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using Proyecto26;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class SignInScreen : Screen
{
    public const string Id = "SignIn";
    
    public TransitionElement closeButton;
    public CharacterDisplay characterDisplay;
    
    public InputField signInIdInput;
    public InputField signInPasswordInput;
    public InteractableMonoBehavior signUpButton;
    public InteractableMonoBehavior signInButton;

    public InputField signUpIdInput;
    public InputField signUpPasswordInput;
    public InputField signUpEmailInput;

    public TransitionElement signInCardParent;
    public TransitionElement signUpCardParent;
    public TransitionElement signInButtonParent;
    public TransitionElement signUpButtonParent;

    private bool lastAuthenticateSucceeded = false; 
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        signInIdInput.text = Context.Player.Id;
        signUpButton.onPointerClick.SetListener(_ => SwitchToSignUp());
        signInButton.onPointerClick.SetListener(_ => SwitchToSignIn());
    }

    public void SwitchToSignUp()
    {
        signUpButtonParent.Leave();
        signInButtonParent.Enter();
        signInCardParent.Leave();
        signUpCardParent.Enter();
    }

    public void SwitchToSignIn()
    {
        signInButtonParent.Leave();
        signUpButtonParent.Enter();
        signUpCardParent.Leave();
        signInCardParent.Enter();
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        characterDisplay.Load(CharacterAsset.GetTachieBundleId("Sayaka"));
    }

    public async UniTask SignIn()
    {
        if (signInIdInput.text.IsNullOrEmptyTrimmed())
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_ID".Get());
            return;
        }
        if (signInPasswordInput.text.IsNullOrEmptyTrimmed())
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_PASSWORD".Get());
            return;
        }

        var id = signInIdInput.text = signInIdInput.text.ToLower(CultureInfo.InvariantCulture).Trim();
        var password = signInPasswordInput.text;

        var completed = false;
        Authenticate(id, password, false).Finally(() => completed = true);

        closeButton.Leave();
        signUpButtonParent.Leave();
        await UniTask.WaitUntil(() => completed);
        if (State != ScreenState.Active) return;
        closeButton.Enter();
        signUpButtonParent.Enter();
    }
    
    public async UniTask SignUp()
    {
        if (signUpIdInput.text.IsNullOrEmptyTrimmed())
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_ID".Get());
            return;
        }
        if (signUpPasswordInput.text.IsNullOrEmptyTrimmed())
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_PASSWORD".Get());
            return;
        }
        if (signUpEmailInput.text.IsNullOrEmptyTrimmed())
        {
            Toast.Next(Toast.Status.Failure, "TOAST_ENTER_EMAIL_ADDRESS".Get());
            return;
        }
        
        var id = signUpIdInput.text = signUpIdInput.text.ToLower(CultureInfo.InvariantCulture).Trim();
        var password = signUpPasswordInput.text;
        var email = signUpEmailInput.text;
        
        if (!Regex.IsMatch(id, "^[a-z0-9-_]{3,16}$"))
        {
            Toast.Next(Toast.Status.Failure, "TOAST_INCORRECT_ID_FORMAT".Get());
            return;
        }
        if (password.Length < 9)
        {
            Toast.Next(Toast.Status.Failure, "TOAST_INCORRECT_PASSWORD_FORMAT".Get());
            return;
        }
        if (!IsValidEmail(email))
        {
            Toast.Next(Toast.Status.Failure, "TOAST_INVALID_EMAIL_ADDRESS".Get());
            return;
        }
        
        if (!await TermsOverlay.Show("TERMS_OF_SERVICE".Get()))
        {
            return;
        }

        var registered = false;
        var failed = false;
        RestClient.Put(new RequestHelper
            {
                Uri = Context.ApiUrl + $"/session?captcha={SecuredOperations.GetCaptcha()}",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true,
                Body = new SignUpBody
                {
                    uid = id,
                    password = password,
                    email = email
                }
            })
            .Then(_ =>
            {
                registered = true;
            })
            .CatchRequestError(error =>
            {
                failed = true;
                Debug.LogError(error);

                var errorResponse = JsonConvert.DeserializeObject<SignUpErrorResponse>(error.Response);
                Toast.Next(Toast.Status.Failure, errorResponse.message);
            });

        closeButton.Leave();
        signInButtonParent.Leave();
        await UniTask.WaitUntil(() => registered || failed);
        if (failed)
        {
            closeButton.Enter();
            signInButtonParent.Enter();
            return;
        }
        
        var completed = false;
        Authenticate(id, password, true).Finally(() =>
        {
            completed = true;
            signUpIdInput.text = "";
            signUpPasswordInput.text = "";
            signUpEmailInput.text = "";
            signInIdInput.text = id;
            if (!lastAuthenticateSucceeded)
            {
                signInPasswordInput.text = password;
                SwitchToSignIn();
            }
        });

        await UniTask.WaitUntil(() => completed);
        if (State != ScreenState.Active) return;
        closeButton.Enter();
        signInButtonParent.Enter();
    }

    private RSG.IPromise Authenticate(string id, string password, bool signUp)
    {
        Context.Player.Settings.PlayerId = id;
        Context.Player.SaveSettings();
        return Context.OnlinePlayer.Authenticate(password)
            .Then(profile =>
            {
                if (profile == null)
                {
                    lastAuthenticateSucceeded = false;
                    Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
                    return;
                }

                lastAuthenticateSucceeded = true;
                Toast.Next(Toast.Status.Success, (signUp ? "TOAST_SUCCESSFULLY_SIGNED_UP" : "TOAST_SUCCESSFULLY_SIGNED_IN").Get());
                ProfileWidget.Instance.SetSignedIn(profile);
                Context.AudioManager.Get("ActionSuccess").Play();
                Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.In,
                    addTargetScreenToHistory: false);
            })
            .CatchRequestError(error =>
            {
                lastAuthenticateSucceeded = false;
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
            });
    }
    
    /**
     * Credits: https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
     */
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                var domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email,
                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    [Serializable]
    class SignUpBody
    {
        public string uid;
        public string email;
        public string password;
    }

    [Serializable]
    class SignUpErrorResponse
    {
        public string message;
    }

}