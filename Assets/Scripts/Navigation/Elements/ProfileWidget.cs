using System;
using System.Collections.Generic;
using DG.Tweening;
using ICSharpCode.SharpZipLib.Zip;
using MoreMountains.NiceVibrations;
using Proyecto26;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ProfileWidget : SingletonMonoBehavior<ProfileWidget>, ScreenChangeListener
{
    [GetComponent] public RectTransform rectTransform;
    public HorizontalLayoutGroup layoutGroup;
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public Image background;
    public Image avatarImage;
    public SpinnerElement spinner;
    public new Text name;
    public LayoutGroup infoLayoutGroup;
    public Text rating;
    public Text level;
    
    private Vector2 startAnchoredPosition;
    private Profile lastProfile;

    protected override void Awake()
    {
        SetSignedOut();
        infoLayoutGroup.gameObject.SetActive(false);
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.ProfileWidget;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    private async void Start()
    {
        startAnchoredPosition = rectTransform.anchoredPosition;
        Context.ScreenManager.AddHandler(this);
        Context.OnlinePlayer.OnProfileChanged.AddListener(profile =>
        {
            UpdateRatingAndLevel(profile, Context.ScreenManager.ActiveScreen.GetId() == ResultScreen.Id);
        });
        await UniTask.WaitUntil(() => this == null || Context.ScreenManager.ActiveScreen != null);
        if (this == null) return;

        if (Context.ScreenManager.ActiveScreen.GetId() != MainMenuScreen.Id)
        {
            Shrink();
        }
    }

    private new void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
    }

    public void Enter()
    {
        FadeIn();
        if (Context.OnlinePlayer.IsAuthenticating) return;
        if (Context.OnlinePlayer.IsAuthenticated)
        {
            SetSignedIn(Context.OnlinePlayer.LastProfile);
        }
        else if (!Context.OnlinePlayer.IsAuthenticated && !Context.OnlinePlayer.IsAuthenticating &&
                 !string.IsNullOrEmpty(Context.Player.Settings.LoginToken))
        {
            SetSigningIn();
        }
        else
        {
            SetSignedOut();
        }
    }

    public void Enlarge()
    {
        rectTransform.DOAnchorPos(startAnchoredPosition, 0.4f);
        transform.DOScale(1f, 0.4f);
    }

    public void Shrink()
    {
        rectTransform.DOAnchorPos(startAnchoredPosition + new Vector2(12, 20), 0.4f);
        transform.DOScale(0.9f, 0.4f);
    }

    public void SetSigningIn()
    {
        name.text = "PROFILE_WIDGET_SIGNING_IN".Get();
        name.DOKill();
        name.color = name.color.WithAlpha(1);
        name.DOFade(0, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        infoLayoutGroup.gameObject.SetActive(false);
        LayoutFixer.Fix(layoutGroup.transform);
        background.color = Color.white;
        spinner.IsSpinning = true;

        if (Context.IsOffline())
        {
            Context.OnlinePlayer.FetchProfile().Then(it =>
            {
                if (it == null)
                {
                    SetSignedOut();
                    Context.OnlinePlayer.Deauthenticate();
                }
                else
                {
                    SetSignedIn(Context.OnlinePlayer.LastProfile = it);
                    Context.OnlinePlayer.IsAuthenticated = true;
                }
            });
            return;
        }
        
        Context.OnlinePlayer.AuthenticateWithJwtToken()
            .Then(profile =>
            {
                Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_SIGNED_IN".Get());
                SetSignedIn(profile);
                Context.OnlinePlayer.IsAuthenticated = true;

                if (Context.IsOffline()) Context.SetOffline(false);
            })
            .CatchRequestError(error =>
            {
                Context.OnlinePlayer.IsAuthenticated = false;
                if (error.IsNetworkError)
                {
                    Context.Haptic(HapticTypes.Warning, true);
                    var dialog = Dialog.Instantiate();
                    dialog.Message = "DIALOG_USE_OFFLINE_MODE".Get();
                    dialog.UseProgress = false;
                    dialog.UsePositiveButton = true;
                    dialog.UseNegativeButton = true;
                    dialog.OnPositiveButtonClicked = _ =>
                    {
                        Context.SetOffline(true);
                        Context.OnlinePlayer.FetchProfile().Then(it =>
                        {
                            if (it == null)
                            {
                                SetSignedOut();
                                Context.OnlinePlayer.Deauthenticate();
                            }
                            else
                            {
                                SetSignedIn(Context.OnlinePlayer.LastProfile = it);
                                Context.OnlinePlayer.IsAuthenticated = true;
                            }
                            dialog.Close();
                        }).Catch(exception => throw new InvalidOperationException()); // Impossible
                    };
                    dialog.OnNegativeButtonClicked = _ =>
                    {
                        dialog.Close();
                        SetSigningIn();
                    };
                    dialog.Open();
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(error.Response);
                    Toast.Next(Toast.Status.Failure, errorResponse.message);
                    Debug.LogWarning("Sign in failed.");
                    Debug.LogWarning(error);
                    SetSignedOut();
                }
            });
    }

    public async void SetSignedIn(Profile profile)
    {
        name.text = profile.User.Uid;
        name.DOKill();
        name.DOFade(1, 0.2f);
        infoLayoutGroup.gameObject.SetActive(true);
        LayoutFixer.Fix(layoutGroup.transform);
        if (avatarImage.sprite == null)
        {
            spinner.IsSpinning = true;
            var url = profile.User.Avatar.LargeUrl;
            print("Avatar URL: " + url);
            var sprite = await Context.AssetMemory.LoadAsset<Sprite>(
                url, 
                AssetTag.PlayerAvatar,
                useFileCacheOnly: Context.IsOffline()
            );
            spinner.IsSpinning = false;
            if (sprite != null)
            {
                SetAvatarSprite(sprite);
            }
            else
            {
                if (Context.IsOnline())
                {
                    Toast.Enqueue(Toast.Status.Failure, "TOAST_COULD_NOT_DOWNLOAD_AVATAR".Get());
                }
            }
        }
        UpdateRatingAndLevel(profile);
    }

    public void SetAvatarSprite(Sprite sprite)
    {
        avatarImage.sprite = sprite;
        avatarImage.DOColor(Color.white, 0.4f).OnComplete(() =>
        {
            background.color = Color.clear;
            spinner.defaultIcon.GetComponent<Image>().color = Color.clear;
        });
    }

    public void SetSignedOut()
    {
        name.text = "PROFILE_WIDGET_NOT_SIGNED_IN".Get();
        name.DOKill();
        name.DOFade(1, 0.2f);
        infoLayoutGroup.gameObject.SetActive(false);
        LayoutFixer.Fix(layoutGroup.transform);
        background.color = Color.white;
        spinner.defaultIcon.GetComponent<Image>().color = Color.white;
        spinner.IsSpinning = false;
        avatarImage.sprite = null;
        avatarImage.color = Color.clear;
    }

    public static readonly HashSet<string> HiddenScreenIds = new HashSet<string> {SignInScreen.Id, ProfileScreen.Id, SettingsScreen.Id, CharacterSelectionScreen.Id};
    public static readonly HashSet<string> StaticScreenIds = new HashSet<string> {ResultScreen.Id, TierBreakScreen.Id, TierResultScreen.Id};

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from != null && from.GetId() == MainMenuScreen.Id)
        {
            Shrink();
        }
        else if (to.GetId() == MainMenuScreen.Id)
        {
            Enlarge();
        }

        if (HiddenScreenIds.Contains(to.GetId()))
        {
            FadeOut();
        }

        if (StaticScreenIds.Contains(to.GetId()))
        {
            canvasGroup.blocksRaycasts = canvasGroup.interactable = false;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from != null && HiddenScreenIds.Contains(from.GetId()) && !HiddenScreenIds.Contains(to.GetId()))
        {
            FadeIn();
        }

        if (StaticScreenIds.Contains(to.GetId()))
        {
            canvasGroup.blocksRaycasts = canvasGroup.interactable = false;
        }

        if (Context.OnlinePlayer.LastProfile != null)
        {
            UpdateRatingAndLevel(Context.OnlinePlayer.LastProfile);
        }
    }

    public void FadeIn()
    {
        canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
        canvasGroup.blocksRaycasts = canvasGroup.interactable =
            !StaticScreenIds.Contains(Context.ScreenManager.ActiveScreen.GetId());
    }

    public void FadeOut()
    {
        canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        canvasGroup.blocksRaycasts = canvasGroup.interactable = false;
    }

    public void UpdateRatingAndLevel(Profile profile, bool showChange = false)
    {
        rating.text = $"{"PROFILE_WIDGET_RATING".Get()} {profile.Rating:N2}";
        level.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {profile.Exp.CurrentLevel}";

        if (showChange && lastProfile != null)
        {
            var lastRating = Math.Floor(lastProfile.Rating * 100) / 100;
            var currentRating = Math.Floor(profile.Rating * 100) / 100;
            var rtDifference = currentRating - lastRating;
            if (rtDifference >= 0.01)
            {
                rating.text += $" <color=#9BC53D>(+{Math.Round(rtDifference, 2)})</color>";
            } 
            /*else if (rtDifference <= -0.01)
            {
                rating.text += $" <color=#E55934>({Math.Round(rtDifference, 2)})</color>";
            }*/

            var levelDifference = profile.Exp.CurrentLevel - lastProfile.Exp.CurrentLevel;
            if (levelDifference > 0)
            {
                level.text += $" <color=#9BC53D>(+{levelDifference})</color>";
            }
        }
        
        lastProfile = profile;
        if (layoutGroup != null) LayoutFixer.Fix(layoutGroup.transform);
    }

}