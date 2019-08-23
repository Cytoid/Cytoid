using System;
using System.Collections.Generic;
using DG.Tweening;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
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

    protected override void Awake()
    {
        SetSignedOut();
    }

    private async void Start()
    {
        Context.ScreenManager.AddHandler(this);
        startLocalPosition = transform.localPosition;
        infoLayoutGroup.gameObject.SetActive(false);
        transform.RebuildLayout();

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
        rating.text = "Rating " + profile.rating.ToString("N2");
        level.text = "Level " + profile.exp.currentLevel;
        infoLayoutGroup.gameObject.SetActive(true);
        LayoutFixer.Fix(layoutGroup.transform);
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
                        Context.SpriteCache.PutSprite("avatar://" + profile.user.uid, "avatar", sprite);
                        SetAvatarSprite(sprite);
                    }).Catch(error => { Toast.Enqueue(Toast.Status.Failure, "Could not download the avatar."); })
                    .Finally(() => spinner.IsSpinning = false);
            }
        }
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

    private static List<string> HiddenScreenIds = new List<string> {SignInScreen.Id, ProfileScreen.Id};

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from.GetId() == MainMenuScreen.Id && !HiddenScreenIds.Contains(to.GetId()))
        {
            Shrink();
        }
        else if (to.GetId() == MainMenuScreen.Id)
        {
            Enlarge();
        }

        if (HiddenScreenIds.Contains(to.GetId()))
        {
            canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (HiddenScreenIds.Contains(from.GetId()))
        {
            canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
            canvasGroup.blocksRaycasts = true;
        }
    }
}