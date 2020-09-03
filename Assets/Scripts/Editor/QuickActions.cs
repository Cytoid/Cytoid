using System.Collections.Generic;
using System.IO;
using Ink.Runtime;
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
    [ToggleLeft] public bool firstOccurrence;
    [ToggleLeft] public bool signedIn;
    public float rating;
    
    [Button(Name = "Story: Training")]
    [DisableInEditorMode]
    public async void StoryTrainingIntro()
    {
        if (running) DialogueOverlay.TerminateCurrentStory = true;
        var compiler = new Ink.Compiler(File.ReadAllText("Assets/Resources/Stories/Training.ink"));
        var story = compiler.Compile();
        story.variablesState["FirstOccurrence"] = firstOccurrence;
        story.variablesState["SignedIn"] = signedIn;
        story.variablesState["Rating"] = rating;
        running = true;
        await DialogueOverlay.Show(story);
        running = false;
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
    
}