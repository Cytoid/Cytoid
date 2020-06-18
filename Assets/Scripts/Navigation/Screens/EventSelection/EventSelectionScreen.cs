using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class EventSelectionScreen : Screen
{
    public Image coverImage;
    public Image logoImage;

    public TransitionElement infoBanner;
    public Text durationText;
    public InteractableMonoBehavior viewDetailsButton;
    public InteractableMonoBehavior enterButton;
    
    public InteractableMonoBehavior helpButton;
    public InteractableMonoBehavior previousButton;
    public InteractableMonoBehavior nextButton;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        coverImage.sprite = null;
        logoImage.sprite = null;
        helpButton.onPointerClick.SetListener(_ => Dialog.PromptAlert("EVENT_TUTORIAL".Get()));
    }

    public override void OnScreenBecameActive()
    {
        coverImage.color = Color.black;
        logoImage.color = Color.white.WithAlpha(0);
        
        base.OnScreenBecameActive();
    }

    protected override async void LoadPayload(ScreenLoadPromise promise)
    {
        SpinnerOverlay.Show();
        await Context.LevelManager.LoadLevelsOfType(LevelType.User);

        RestClient.GetArray<EventMeta>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/events",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).Then(data =>
            {
                if (data.Length == 0)
                {
                    Dialog.PromptGoBack("DIALOG_EVENTS_NOT_AVAILABLE".Get());
                    promise.Reject();
                    return;
                }
                IntentPayload.Events = data.ToList();
                promise.Resolve(IntentPayload);
            })
            .CatchRequestError(error =>
            {
                Debug.LogError(error);
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                
                promise.Reject();
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    protected override void Render()
    {
        previousButton.onPointerClick.RemoveAllListeners();
        nextButton.onPointerClick.RemoveAllListeners();
        if (LoadedPayload.Events.Count == 1)
        {
            previousButton.scaleOnClick = false;
            nextButton.scaleOnClick = false;
            previousButton.GetComponentInChildren<Image>().SetAlpha(0.3f);
            nextButton.GetComponentInChildren<Image>().SetAlpha(0.3f);
        }
        else
        {
            previousButton.scaleOnClick = true;
            nextButton.scaleOnClick = true;
            previousButton.GetComponentInChildren<Image>().SetAlpha(1f);
            nextButton.GetComponentInChildren<Image>().SetAlpha(1f);
            previousButton.onPointerClick.AddListener(_ => PreviousEvent());
            nextButton.onPointerClick.AddListener(_ => NextEvent());
        }
        base.Render();
    }

    protected override void OnRendered()
    {
        base.OnRendered();
        
        if (LoadedPayload.SelectedIndex < 0 || LoadedPayload.SelectedIndex >= LoadedPayload.Events.Count) LoadedPayload.SelectedIndex = 0;
        LoadEvent(LoadedPayload.Events[LoadedPayload.SelectedIndex]);
    }

    public void LoadEvent(EventMeta meta)
    {
        async UniTask LoadCover(CancellationToken token)
        {
            Assert.IsNotNull(meta.cover);
            
            var tasks = new List<UniTask>();
            if (coverImage.sprite != null)
            {
                coverImage.DOKill();
                coverImage.DOColor(Color.black, 0.4f);
                tasks.Add(UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: token));
            }
            var downloadTask = Context.AssetMemory.LoadAsset<Sprite>(meta.cover.OriginalUrl, AssetTag.EventCover, allowFileCache: true, cancellationToken: token);
            tasks.Add(downloadTask);

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                return;
            }

            coverImage.sprite = downloadTask.Result;
            coverImage.FitSpriteAspectRatio();
            coverImage.DOColor(Color.white, 2f).SetDelay(0.8f);
        }
        async UniTask LoadLogo(CancellationToken token)
        {
            Assert.IsNotNull(meta.logo);
            
            var tasks = new List<UniTask>();
            if (logoImage.sprite != null)
            {
                logoImage.DOKill();
                logoImage.DOFade(0, 0.4f);
                tasks.Add(UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: token));
            }
            var downloadTask = Context.AssetMemory.LoadAsset<Sprite>(meta.logo.OriginalUrl, AssetTag.EventLogo, allowFileCache: true, cancellationToken: token);
            tasks.Add(downloadTask);

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                return;
            }

            logoImage.sprite = downloadTask.Result;
            logoImage.DOFade(1, 0.8f).SetDelay(0.4f);
        }
        AddTask(LoadCover);
        AddTask(LoadLogo);
        
        infoBanner.Leave(onComplete: () =>
        {
            viewDetailsButton.onPointerClick.SetListener(_ => Application.OpenURL($"{Context.WebsiteUrl}/posts/{meta.uid}"));
            const string dateFormat = "yyyy/MM/dd HH:mm";
            durationText.text = (meta.startDate.HasValue ? meta.startDate.Value.LocalDateTime.ToString(dateFormat) : "")
                                + "~"
                                + (meta.endDate.HasValue ? meta.endDate.Value.LocalDateTime.ToString(dateFormat) : "");
            enterButton.onPointerClick.SetListener(_ =>
            {
                if (meta.locked)
                {
                    Context.Haptic(HapticTypes.Failure, true);
                    // TODO
                    Dialog.Instantiate().Also(it =>
                    {
                        it.Message = "TEMP_MESSAGE_NEW_VERSION_REQUIRED".Get();
                        it.UsePositiveButton = true;
                        it.UseNegativeButton = false;
                    }).Open();
                    return;
                }
                Context.Haptic(HapticTypes.SoftImpact, true);
                if (meta.levelId != null)
                {
                    SpinnerOverlay.Show();

                    RestClient.Get<OnlineLevel>(new RequestHelper
                    {
                        Uri = $"{Context.ApiUrl}/levels/{meta.levelId}"
                    }).Then(level =>
                    {
                        Context.ScreenManager.ChangeScreen(
                            GamePreparationScreen.Id, ScreenTransition.In, 0.4f,
                            transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter(),
                            payload: new GamePreparationScreen.Payload {Level = level.ToLevel(LevelType.User)}
                        );
                    }).CatchRequestError(error =>
                    {
                        Debug.LogError(error);
                        Dialog.PromptAlert("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                    }).Finally(() => SpinnerOverlay.Hide());
                } 
                else if (meta.collectionId != null)
                {
                    Context.ScreenManager.ChangeScreen(
                        CollectionDetailsScreen.Id, ScreenTransition.In, 0.4f,
                        transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter(),
                        payload: new CollectionDetailsScreen.Payload
                            {CollectionId = meta.collectionId, Type = LevelType.User}
                    );
                }
            });
            infoBanner.transform.RebuildLayout();
            infoBanner.Enter();
        });
    }

    private void NextEvent()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex + 1).Mod(LoadedPayload.Events.Count);
        LoadEvent(LoadedPayload.Events[LoadedPayload.SelectedIndex]);
    }

    private void PreviousEvent()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex - 1).Mod(LoadedPayload.Events.Count);
        LoadEvent(LoadedPayload.Events[LoadedPayload.SelectedIndex]);
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            coverImage.DOKill();
            coverImage.sprite = null;
            logoImage.DOKill();
            logoImage.sprite = null;
            if (to is MainMenuScreen)
            {
                Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.EventCover);
                Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.EventLogo);
                Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.RemoteLevelCoverThumbnail);

                LoadedPayload = null;
            }
        }
    }

    public class Payload : ScreenPayload
    {
        public List<EventMeta> Events;
        public int SelectedIndex;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }
    public override ScreenPayload GetDefaultPayload() => new Payload();
    
    public const string Id = "EventSelection";
    public override string GetId() => Id;

}