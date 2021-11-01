using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Proyecto26;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ProfileScreen : Screen
{
    public UpperOverlay upperOverlay;
    public ContentTabs contentTabs;
    public Transform topRightColumn;
    public LeaderboardContainer leaderboard;
    public RadioGroup leaderboardModeSelect;
    public ScrollRect leaderboardScrollRect;
    public InteractableMonoBehavior signOutButton;
    public InteractableMonoBehavior toggleOfflineButton;

    public ProfileTab profileTab;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        signOutButton.onPointerClick.AddListener(_ =>
        {
            Context.OnlinePlayer.Deauthenticate();
            Context.SetOffline(false);
            Context.ScreenManager.History.Clear();
            Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_SIGNED_OUT".Get());
            ProfileWidget.Instance.SetSignedOut();
            Context.ScreenManager.GetScreen<SignInScreen>().signInPasswordInput.text = "";
        });
        contentTabs.selectOnScreenBecameActive = false;
        contentTabs.onTabSelect.AddListener((index, tab) =>
        {
            upperOverlay.contentRect = contentTabs.viewportContents[index];
            if (index == 1)
            {
                if (Context.IsOnline()) UpdateLeaderboard(leaderboardModeSelect.Value);
            }
        });
        leaderboardModeSelect.onSelect.AddListener(UpdateLeaderboard);
        leaderboardScrollRect.gameObject.AddComponent<ScrollRectFocusHelper>();
        toggleOfflineButton.onPointerClick.AddListener(_ => ToggleOffline());
    }

    public override void OnScreenBecameActive()
    {
        profileTab.characterTransitionElement.Leave(false, true);
        profileTab.changeCharacterButton.gameObject.SetActive(false);
        contentTabs.UnselectAll();
        contentTabs.gameObject.SetActive(false);
        topRightColumn.gameObject.SetActive(false);
        base.OnScreenBecameActive();
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        if (LoadedPayload != null)
        {
            LoadedPayload.TabIndex = contentTabs.SelectedIndex;
            print($"Location: {profileTab.scrollRect.verticalNormalizedPosition}");
            LoadedPayload.ProfileScrollPosition = profileTab.scrollRect.verticalNormalizedPosition;
            LoadedPayload.LeaderboardScrollPosition = leaderboardScrollRect.verticalNormalizedPosition;
        }
    }

    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        var dy = profileTab.characterPaddingReference.GetScreenSpaceRect().min.y - 64;
        dy = Math.Max(0, dy);
        dy /= UnityEngine.Screen.height;
        var canvasHeight = 1920f / UnityEngine.Screen.width * UnityEngine.Screen.height;
        dy *= canvasHeight;
        profileTab.characterTransitionElement.rectTransform.SetAnchoredY(dy);
        profileTab.changeCharacterButton.SetAnchoredY(64 + dy);
    }

    public void ToggleOffline()
    {
        Context.ScreenManager.History.Clear();
        Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.Out);
        if (Context.IsOnline())
        {
            Context.SetOffline(true);
            Toast.Enqueue(Toast.Status.Success, "TOAST_SWITCHED_TO_OFFLINE_MODE".Get());
        }
        else
        {
            Context.SetOffline(false);
            ProfileWidget.Instance.SetSigningIn();
        }
    }

    public void ClearLeaderboard()
    {
        leaderboardScrollRect.normalizedPosition = new Vector2(0, 1);
        leaderboard.Clear();
    }

    public void UpdateLeaderboard(string mode)
    {
        ClearLeaderboard();

        SpinnerOverlay.Show();
        var uri = Context.ApiUrl + "/leaderboard?limit=50";
        if (mode == "me") uri += "&user=" + Context.OnlinePlayer.LastProfile.User.Id;
        RestClient.GetArray<Leaderboard.Entry>(new RequestHelper
        {
            Uri = uri,
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(SetData).CatchRequestError(Debug.LogError).Finally(() => SpinnerOverlay.Hide());

        async void SetData(Leaderboard.Entry[] data)
        {
            try
            {
                leaderboard.SetData(data);
                if (mode == "me")
                {
                    var meEntry = leaderboard.Entries.Find(it => it.Model.Uid == Context.Player.Id);
                    if (meEntry != null)
                    {
                        await UniTask.DelayFrame(0);
                        leaderboardScrollRect.GetComponent<ScrollRectFocusHelper>()
                            .CenterOnItem(meEntry.transform as RectTransform);
                    }
                }
                await UniTask.DelayFrame(2);
                if (LoadedPayload.LeaderboardScrollPosition > -1) leaderboardScrollRect.verticalNormalizedPosition = LoadedPayload.LeaderboardScrollPosition;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this && to is MainMenuScreen)
        {
            ClearLeaderboard();
        }
    }

    protected override async void LoadPayload(ScreenLoadPromise promise)
    {
        IntentPayload.IsPlayer = IntentPayload.Id == Context.OnlinePlayer.LastProfile?.User.Id;
        if (IntentPayload.IsPlayer)
        {
            if (Context.OnlinePlayer.LastFullProfile != null)
            {
                // Fetch latest character exp
                await Context.CharacterManager.FetchSelectedCharacterExp();
                IntentPayload.Profile = Context.OnlinePlayer.LastFullProfile;
                promise.Resolve(IntentPayload);
                return;
            }

            if (Context.IsOffline())
            {
                // Fetch offline profile and cast it as full profile
                Context.OnlinePlayer.FetchProfile()
                    .Then(profile =>
                    {
                        var fullProfile =
                            JsonConvert.DeserializeObject<FullProfile>(JsonConvert.SerializeObject(profile));
                        IntentPayload.Profile = fullProfile;
                        promise.Resolve(IntentPayload);
                    })
                    .CatchRequestError(error =>
                    {
                        Debug.LogError(error);
                        Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                    });
                return;
            }
        } 
        else if (IntentPayload.Profile != null)
        {
            promise.Resolve(IntentPayload);
            return;
        }
        
        SpinnerOverlay.Show();

        RestClient.Get<FullProfile>(new RequestHelper
        {
            Uri = $"{Context.ApiUrl}/profile/{IntentPayload.Id}/details",
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(async profile =>
        {
            if (IntentPayload.IsPlayer)
            {
                Context.OnlinePlayer.LastFullProfile = profile;

                if (profile.Character.AssetId != Context.CharacterManager.SelectedCharacterId)
                {
                    if (!await Context.BundleManager.DownloadAndSaveCatalog())
                    {
                        Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.Out, addTargetScreenToHistory: false);
                        return;
                    }
                    if (await Context.CharacterManager.SetActiveCharacter(profile.Character.AssetId) == null)
                    {
                        var (success, locallyResolved) = await Context.CharacterManager.DownloadCharacterAssetDialog(CharacterAsset.GetMainBundleId(profile.Character.AssetId));
                        if (!success && !locallyResolved)
                        {
                            Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.Out, addTargetScreenToHistory: false);
                            return;
                        }
                        else
                        {
                            Context.CharacterManager.SelectedCharacterId = profile.Character.AssetId;
                        }
                    }
                }
                
                // Fetch latest character exp
                await Context.CharacterManager.FetchSelectedCharacterExp();
            }

            IntentPayload.Profile = profile;
            promise.Resolve(IntentPayload);
            
            // RestClient.Get<CharacterMeta.ExpData>(new RequestHelper
            // {
            //     Uri = $"{Context.ApiUrl}/characters/exp?characterId={characterId}",
            //     Headers = Context.OnlinePlayer.GetRequestHeaders(),
            //     EnableDebug = true
            // }).Then(expData =>
            // {
            //     profile.Character.Exp = expData;
            // }).CatchRequestError(error =>
            // {
            //     if (error.IsHttpError)
            //     {
            //         Debug.LogError(error);
            //         Context.Player.Settings.ActiveCharacterId = BuiltInData.DefaultCharacterAssetId;
            //     }
            // }).Finally(() =>
            // {
            //     IntentPayload.Profile = profile;
            //     promise.Resolve(IntentPayload);
            // });
        }).CatchRequestError(error =>
        {
            Debug.LogError(error);
            Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
        }).Finally(() => SpinnerOverlay.Hide());
    }

    protected override void Render()
    {
        profileTab.SetModel(LoadedPayload.Profile);
        base.Render();
    }

    protected override async void OnRendered()
    {
        base.OnRendered();
        
        toggleOfflineButton.GetComponentInChildren<Text>().text = Context.IsOnline() ? "OFFLINE_GO_OFFLINE".Get() : "OFFLINE_GO_ONLINE".Get();
        LayoutFixer.Fix(toggleOfflineButton.transform);
        profileTab.characterTransitionElement.Enter();
        profileTab.changeCharacterButton.gameObject.SetActive(LoadedPayload.IsPlayer);
        contentTabs.gameObject.SetActive(LoadedPayload.IsPlayer);
        topRightColumn.gameObject.SetActive(LoadedPayload.IsPlayer);
        // Always use local character if local player
        if (LoadedPayload.Profile.User.Uid == Context.Player.Id)
        {
            profileTab.characterDisplay.Load(CharacterAsset.GetTachieBundleId(Context.CharacterManager.SelectedCharacterId));
        }
        else if (LoadedPayload.Profile.Character != null)
        {
            profileTab.characterDisplay.Load(CharacterAsset.GetTachieBundleId(LoadedPayload.Profile.Character.AssetId));
        }
        var (name, exp) = await Context.CharacterManager.FetchSelectedCharacterExp(useLocal: true);
        profileTab.UpdateCharacterMeta(name, exp);
        contentTabs.Select(LoadedPayload.TabIndex);
        await UniTask.DelayFrame(2);
        if (LoadedPayload.ProfileScrollPosition > -1) profileTab.scrollRect.verticalNormalizedPosition = LoadedPayload.ProfileScrollPosition;
    }

    public class Payload : ScreenPayload
    {
        public string Id;
        public bool IsPlayer;
        public int TabIndex;
        public FullProfile Profile;
        public float ProfileScrollPosition = -1;
        public float LeaderboardScrollPosition = -1;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }

    public const string Id = "Profile";

    public override string GetId() => Id;
}