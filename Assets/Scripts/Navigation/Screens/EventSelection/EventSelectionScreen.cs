using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class EventSelectionScreen : Screen
{
    public const string Id = "EventSelection";
    public static Content LoadedContent;
    private static int lastSelectedIndex = -1;
    
    public override string GetId() => Id;

    public Image coverImage;
    public Image logoImage;

    public TransitionElement infoBanner;
    public Text durationText;
    public InteractableMonoBehavior viewDetailsButton;
    public InteractableMonoBehavior enterButton;
    
    public InteractableMonoBehavior helpButton;
    public InteractableMonoBehavior previousButton;
    public InteractableMonoBehavior nextButton;

    private List<EventMeta> events;
    private int selectedIndex;
    private DateTimeOffset loadToken;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        coverImage.sprite = null;
        logoImage.sprite = null;
        helpButton.onPointerClick.SetListener(_ => Dialog.PromptAlert("EVENT_TUTORIAL".Get()));
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        coverImage.color = Color.black;
        logoImage.color = Color.white.WithAlpha(0);

        if (LoadedContent != null)
        {
            if (lastSelectedIndex >= 0)
            {
                selectedIndex = lastSelectedIndex;
            }
            OnContentLoaded(LoadedContent);
        }
        else
        {
            LoadContent();
        }
        
        return;
        
        SpinnerOverlay.Show();
        SpinnerOverlay.Hide();

        /*Context.CharacterManager.GetAvailableCharactersMeta()
            .Then(characters =>
            {
                if (characters.Count == 0)
                {
                    // TODO: This should not happen! We have Sayaka
                    Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                    return;
                }
                DownloadAvailableCharacters(characters);
            })
            .CatchRequestError(error =>
            {
                if (!error.IsNetworkError)
                {
                    throw error;
                }
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
            })
            .Finally(() => SpinnerOverlay.Hide());*/
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        lastSelectedIndex = selectedIndex;
    }

    public void LoadContent()
    {
        LoadedContent = new Content {Events = new List<EventMeta> {MockData.Event, MockData.Event}};
        OnContentLoaded(LoadedContent);
    }

    public async void OnContentLoaded(Content content)
    {
        events = content.Events;
        
        previousButton.onPointerClick.RemoveAllListeners();
        nextButton.onPointerClick.RemoveAllListeners();
        if (content.Events.Count == 1)
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

        if (selectedIndex < 0 || selectedIndex >= content.Events.Count) selectedIndex = 0;
        LoadEvent(content.Events[selectedIndex]);
    }

    public void LoadEvent(EventMeta meta)
    {
        var token = loadToken = DateTimeOffset.Now;
        async void LoadCover()
        {
            var tasks = new List<UniTask>();
            if (coverImage.sprite != null)
            {
                coverImage.DOKill();
                coverImage.DOColor(Color.black, 0.4f);
                tasks.Add(UniTask.Delay(TimeSpan.FromSeconds(0.4f)));
            }
            var downloadTask = Context.AssetMemory.LoadAsset<Sprite>(meta.coverUrl, AssetTag.EventCover, useFileCache: true);
            tasks.Add(downloadTask);

            await UniTask.WhenAll(tasks);
            if (token != loadToken) return;

            coverImage.sprite = downloadTask.Result;
            coverImage.FitSpriteAspectRatio();
            coverImage.DOColor(Color.white, 2f).SetDelay(0.8f);
        }
        async void LoadLogo()
        {
            var tasks = new List<UniTask>();
            if (logoImage.sprite != null)
            {
                logoImage.DOKill();
                logoImage.DOFade(0, 0.4f);
                tasks.Add(UniTask.Delay(TimeSpan.FromSeconds(0.4f)));
            }
            var downloadTask = Context.AssetMemory.LoadAsset<Sprite>(meta.logoUrl, AssetTag.EventLogo, useFileCache: true);
            tasks.Add(downloadTask);

            await UniTask.WhenAll(tasks);
            if (token != loadToken) return;

            logoImage.sprite = downloadTask.Result;
            logoImage.DOFade(1, 0.8f).SetDelay(0.4f);
        }
        LoadCover();
        LoadLogo();
        
        infoBanner.Leave(onComplete: () =>
        {
            viewDetailsButton.onPointerClick.SetListener(_ => Application.OpenURL(meta.postUrl));
            const string dateFormat = "yyyy/MM/dd HH:mm";
            durationText.text = meta.startDate.ToString(dateFormat) + "~" + meta.endDate.ToString(dateFormat);
            enterButton.onPointerClick.SetListener(_ =>
            {
                if (meta.associatedLevelUid != null)
                {
                    Context.SelectedLevel = meta.associatedLevel.ToLevel(LevelType.Official);
                    Context.ScreenManager.ChangeScreen(
                        GamePreparationScreen.Id, ScreenTransition.In, 0.4f,
                        transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter()
                    );
                } 
                else if (meta.associatedCollectionId != null)
                {
                    CollectionDetailsScreen.LoadedContent = new CollectionDetailsScreen.Content
                        {Id = meta.associatedCollectionId};
                    Context.ScreenManager.ChangeScreen(
                        CollectionDetailsScreen.Id, ScreenTransition.In, 0.4f,
                        transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter()
                    );
                }
            });
            infoBanner.Enter();
        });
    }

    private void NextEvent()
    {
        selectedIndex = (selectedIndex + 1).Mod(events.Count);
        LoadEvent(events[selectedIndex]);
    }

    private void PreviousEvent()
    {
        selectedIndex = (selectedIndex - 1).Mod(events.Count);
        LoadEvent(events[selectedIndex]);
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

                LoadedContent = null;
                loadToken = DateTimeOffset.MinValue;
            }
        }
    }

    public class Content
    {
        public List<EventMeta> Events;
    }

}