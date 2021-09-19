using System;
using System.Collections.Generic;
using System.IO;
using Ink.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class QuickActions : OdinEditorWindow
{
    [MenuItem("Cytoid/Quick Actions")]
    private static void OpenWindow()
    {
        GetWindow<QuickActions>().Show();
    }

    [Button(Name = "Rate on TapTap")]
    [DisableInEditorMode]
    public void RateOnTapTap()
    {
        Context.AudioManager.Get("ActionSuccess").Play();
        Dialog.Prompt("享受 Cytoid 吗？\n请在 TapTap 上给我们打个分吧！", () =>
        {
            Application.OpenURL("https://www.taptap.com/app/158749");
        });
    }

    [Button(Name = "Story: Intro")]
    [DisableInEditorMode]
    public async void StoryIntro()
    {
        var intro = Resources.Load<TextAsset>("Stories/Intro");
        LevelSelectionScreen.HighlightedLevelId = BuiltInData.TutorialLevelId;
        var story = new Story(intro.text);
        story.variablesState["IsBeginner"] = true;
        await DialogueOverlay.Show(story);
        LevelSelectionScreen.HighlightedLevelId = null;
    }

    private bool running;
    [ToggleLeft] public bool isFirstMet;
    [ToggleLeft] public bool signedIn;
    public float rating;

    [Button(Name = "Story: Debug")]
    [DisableInEditorMode]
    public async void StoryDebug()
    {
        var text = Resources.Load<TextAsset>("Stories/Debug");
        var story = new Story(text.text);
        Resources.UnloadAsset(text);
        await DialogueOverlay.Show(story);
    }
    
    [Button(Name = "Clear Training Mode Story State")]
    [DisableInEditorMode]
    public void ClearOneShotShouldIntroduceMechanisms()
    {
        Context.Player.ClearOneShot("Training Mode: Should Introduce Mechanisms");
        Context.Player.ClearOneShot("Training Mode: Is First Met");
    }
    
    [Button(Name = "Story: Training")]
    [DisableInEditorMode]
    public async void StoryTrainingIntro()
    {
        if (running) DialogueOverlay.TerminateCurrentStory = true;
        var compiler = new Ink.Compiler(File.ReadAllText("Assets/Resources/Stories/Training.ink"));
        var story = compiler.Compile();
        story.variablesState["IsFirstMet"] = isFirstMet;
        var shouldIntroduceMechanisms = Context.Player.ShouldOneShot("Training Mode: Should Introduce Mechanisms");
        story.variablesState["ShouldIntroduceMechanisms"] = shouldIntroduceMechanisms;
        story.variablesState["SignedIn"] = signedIn;
        story.variablesState["Rating"] = rating;
        running = true;
        await DialogueOverlay.Show(story);
        running = false;
        if (shouldIntroduceMechanisms && (int) story.variablesState["IntroducedMechanisms"] == 0)
        {
            Context.Player.ClearOneShot("Training Mode: Should Introduce Mechanisms");
        }
    }

    [Button(Name = "Story: Badge")]
    [DisableInEditorMode]
    public async void StoryBadge()
    {
        DialogueOverlay.CurrentBadge = new Badge
        {
            title = "夏祭：一段",
            description = "测试\n你好\n贵阳！\n11111111111",
            type = BadgeType.Event,
            metadata = new Dictionary<string, object>
            {
                {"imageUrl", "http://artifacts.cytoid.io/badges/sora1.jpg"}
            }
        };
        var badge = Resources.Load<TextAsset>("Stories/Badge");
        var story = new Story(badge.text);
        await DialogueOverlay.Show(story);
    }

    [Button(Name = "Story: Practice Mode")]
    [DisableInEditorMode]
    public async void StoryPracticeMode()
    {
        var text = Resources.Load<TextAsset>("Stories/PracticeMode");
        var story = new Story(text.text);
        Resources.UnloadAsset(text);
        await DialogueOverlay.Show(story);
    }
    
    [Button(Name = "Clear Practice Mode State")]
    [DisableInEditorMode]
    public void ClearPracticeModeState()
    {
        Context.Player.ClearOneShot("Practice Mode Explanation");
    }

    [Button(Name = "Terms: Terms of Service")]
    [DisableInEditorMode]
    public async void TermsToS()
    {
        TermsOverlay.Show("TERMS_OF_SERVICE".Get());
    }
    
    [Button(Name = "Terms: Copyright Policy")]
    [DisableInEditorMode]
    public async void TermsCopyrightPolicy()
    {
        TermsOverlay.Show("COPYRIGHT_POLICY".Get());
    }
    
    public CharacterAsset testCharacter;

    [Button(Name = "Preview Test Character")]
    [DisableInEditorMode]
    public void PreviewTestCharacter()
    {
        Context.CharacterManager.SetTestActiveCharacter(testCharacter);
    }
    
    [Button(Name = "Quest Overlay Test")]
    [DisableInEditorMode]
    private static void QuestOverlayTest()
    {
        QuestOverlay.Show(new List<Quest>
        {
            new Quest
            {
                Description = "解锁角色「木苏糖（觉醒）」",
                Objectives = new List<Objective>
                {
                    new Objective
                    {
                        Description = "在关卡「愛を探して」获得 SSS 或以上成绩",
                        IsCompleted = true,
                        CurrentProgress = 1,
                        MaxProgress = 1
                    },
                    new Objective
                    {
                        Description = "角色「木苏糖」等级达到 50 级",
                        IsCompleted = false,
                        CurrentProgress = 47,
                        MaxProgress = 50
                    }
                },
                Rewards = new List<Reward>
                {
                    new Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f38e922fe1dfb383c7b93fa"",""uid"":""sora-1"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora1.jpg""},""type"":""event"",""id"":""5f38e922fe1dfb383c7b93fa""}"))
                    },
                    new Reward()
                    {
                        type = "character",
                        characterValue = new Lazy<CharacterMeta>(() => MockData.AvailableCharacters[MockData.AvailableCharacters.Count - 1])
                    }
                }
            }
        });
    }
    
}