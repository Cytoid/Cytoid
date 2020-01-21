using System;
using System.Collections.Generic;
using DG.Tweening;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ProfileWidget : SingletonMonoBehavior<ProfileWidget>, ScreenChangeListener
{
    public HorizontalLayoutGroup layoutGroup;
    public CanvasGroup canvasGroup;
    public Image background;
    public Image avatarImage;
    public SpinnerElement spinner;
    public new Text name;
    public LayoutGroup infoLayoutGroup;
    public Text rating;
    public Text level;

    private Vector2 startLocalPosition;
    private Profile lastProfile;

    protected override void Awake()
    {
        SetSignedOut();
        infoLayoutGroup.gameObject.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    private async void Start()
    {
        startLocalPosition = transform.localPosition;
        Context.ScreenManager.AddHandler(this);
        Context.OnlinePlayer.OnProfileChanged.AddListener(profile =>
        {
            UpdateRatingAndLevel(profile, Context.ScreenManager.ActiveScreen.GetId() == ResultScreen.Id);
        });
        await UniTask.WaitUntil(() => Context.ScreenManager.ActiveScreen != null);

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
        if (Context.OnlinePlayer.IsAuthenticated)
        {
            SetSignedIn(Context.OnlinePlayer.LastProfile);
        }
        else if (!Context.OnlinePlayer.IsAuthenticated && !Context.OnlinePlayer.IsAuthenticating &&
                 !string.IsNullOrEmpty(Context.OnlinePlayer.GetJwtToken()))
        {
            SetSigningIn();
            Context.OnlinePlayer.AuthenticateWithJwtToken()
                .Then(profile =>
                {
                    Toast.Next(Toast.Status.Success, "Successfully signed in.");
                    SetSignedIn(profile);
                })
                .Catch(error => SetSignedOut())
                .HandleRequestErrors(error => SetSignedOut());
        }
    }

    public void Enlarge()
    {
        transform.DOLocalMove(startLocalPosition, 0.4f);
        transform.DOScale(1f, 0.4f);
    }

    public void Shrink()
    {
        transform.DOLocalMove(startLocalPosition + new Vector2(12, 20), 0.4f);
        transform.DOScale(0.9f, 0.4f);
    }

    public void SetSigningIn()
    {
        name.text = "Signing in...";
        name.DOFade(0, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        infoLayoutGroup.gameObject.SetActive(false);
        LayoutFixer.Fix(layoutGroup.transform);
        background.color = Color.white;
        spinner.IsSpinning = true;
    }

    public void SetSignedIn(Profile profile)
    {
        name.text = profile.user.uid;
        name.DOKill();
        name.DOFade(1, 0.2f);
        infoLayoutGroup.gameObject.SetActive(true);
        infoLayoutGroup.gameObject.SetActive(true);
        if (avatarImage.sprite == null)
        {
            spinner.IsSpinning = true;

            var cachedSprite = Context.SpriteCache.GetCachedSprite("avatar://" + profile.user.uid);
            if (cachedSprite != null)
            {
                SetAvatarSprite(cachedSprite);
                spinner.IsSpinning = false;
            }
            else
            {
                RestClient.Get(new RequestHelper
                    {
                        Uri = profile.user.avatarURL,
                        DownloadHandler = new DownloadHandlerTexture()
                    }).Then(response =>
                    {
                        var texture = ((DownloadHandlerTexture) response.Request.downloadHandler).texture;
                        var sprite = texture.CreateSprite();
                        Context.SpriteCache.PutSprite("avatar://" + profile.user.uid, "UserAvatar", sprite);
                        SetAvatarSprite(sprite);
                    }).Catch(error => { Toast.Enqueue(Toast.Status.Failure, "Could not download the avatar."); })
                    .Finally(() => spinner.IsSpinning = false);
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
        name.text = "Not signed in";
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

    private static List<string> hiddenScreenIds = new List<string> {SignInScreen.Id, ProfileScreen.Id};
    private static List<string> staticScreenIds = new List<string> {ResultScreen.Id};

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from != null && from.GetId() == MainMenuScreen.Id && !hiddenScreenIds.Contains(to.GetId()))
        {
            Shrink();
        }
        else if (to.GetId() == MainMenuScreen.Id)
        {
            Enlarge();
        }

        if (hiddenScreenIds.Contains(to.GetId()))
        {
            FadeOut();
        }

        if (staticScreenIds.Contains(to.GetId()))
        {
            canvasGroup.blocksRaycasts = canvasGroup.interactable = false;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from != null && hiddenScreenIds.Contains(from.GetId()))
        {
            FadeIn();
        }

        if (staticScreenIds.Contains(to.GetId()))
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
            !staticScreenIds.Contains(Context.ScreenManager.ActiveScreen.GetId());
    }

    public void FadeOut()
    {
        canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        canvasGroup.blocksRaycasts = canvasGroup.interactable = false;
    }

    public void UpdateRatingAndLevel(Profile profile, bool showChange = false)
    {
        rating.text = "Rating " + profile.rating.ToString("N2");
        level.text = "Level " + profile.exp.currentLevel;

        if (showChange && lastProfile != null)
        {
            var lastRating = Math.Floor(lastProfile.rating * 100) / 100;
            var currentRating = Math.Floor(profile.rating * 100) / 100;
            var rtDifference = currentRating - lastRating;
            if (rtDifference >= 0.01)
            {
                rating.text += $" <color=#9BC53D>(+{Math.Round(rtDifference, 2)})</color>";
            } 
            else if (rtDifference <= -0.01)
            {
                rating.text += $" <color=#E55934>({Math.Round(rtDifference, 2)})</color>";
            }

            var levelDifference = profile.exp.currentLevel - lastProfile.exp.currentLevel;
            if (levelDifference > 0)
            {
                level.text += $" <color=#9BC53D>(+{levelDifference})</color>";
            }
        }
        
        lastProfile = profile;
        LayoutFixer.Fix(layoutGroup.transform);
    }

}