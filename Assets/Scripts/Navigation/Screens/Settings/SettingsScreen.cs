using System.Linq.Expressions;
using Polyglot;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsScreen : Screen, ScreenChangeListener
{
    public const string Id = "Settings";

    public override string GetId() => Id;

    public UpperOverlay upperOverlay;
    public RectTransform generalTab;
    public RectTransform gameplayTab;
    public RectTransform visualTab;
    public RectTransform advancedTab;
    public ContentTabs contentTabs;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        contentTabs.onTabSelect.AddListener((index, _) =>
        {
            switch (index)
            {
                case 0:
                    upperOverlay.contentRect = generalTab;
                    break;
                case 1:
                    upperOverlay.contentRect = gameplayTab;
                    break;
                case 2:
                    upperOverlay.contentRect = visualTab;
                    break;
                case 3:
                    upperOverlay.contentRect = advancedTab;
                    break;
            }
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        InstantiateSettings();
        Context.OnLanguageChanged.AddListener(() =>
        {
            DestroySettings();
            InstantiateSettings();
        });
    }
    
    private void InstantiateSettings()
    {
        SettingsFactory.InstantiateGeneralSettings(generalTab, true);
        SettingsFactory.InstantiateGameplaySettings(gameplayTab);
        SettingsFactory.InstantiateVisualSettings(visualTab);
        SettingsFactory.InstantiateAdvancedSettings(advancedTab, true);

        async void Fix(Transform transform)
        {
            LayoutStaticizer.Activate(transform);
            LayoutFixer.Fix(transform);
            await UniTask.DelayFrame(5);
            LayoutStaticizer.Staticize(transform);
        }
        Fix(generalTab.parent);
        Fix(gameplayTab.parent);
        Fix(visualTab.parent);
        Fix(advancedTab.parent);
    }

    private void DestroySettings()
    {
        foreach (Transform child in generalTab) Destroy(child.gameObject);
        foreach (Transform child in gameplayTab) Destroy(child.gameObject);
        foreach (Transform child in visualTab) Destroy(child.gameObject);
        foreach (Transform child in advancedTab) Destroy(child.gameObject);
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this) DestroySettings();
    }

}